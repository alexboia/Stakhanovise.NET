// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-201, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueNotificationListener : ITaskQueueNotificationListener,
		IAppMetricsProvider,
		IDisposable
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		public event EventHandler<NewTaskPostedEventArgs> NewTaskPosted;

		public event EventHandler<ListenerConnectedEventArgs> ListenerConnected;

		public event EventHandler<ListenerConnectionRestoredEventArgs> ListenerConnectionRestored;

		public event EventHandler<ListenerTimedOutEventArgs> ListenerTimedOutWhileWaiting;

		private string mSignalingConnectionString;

		private string mNewTaskNotificationChannelName;

		private StateController mStateController =
			new StateController();

		private CancellationTokenSource mStopTokenSource;

		private ManualResetEvent mWaitForFirstStartWaitHandle =
			new ManualResetEvent( initialState: false );

		private Task mNewTaskUpdatesListenerWorker;

		private NpgsqlConnection mSignalingConn = null;

		private int mListenerConnectionBackendProcessId;

		private int mWaitNotificationTimeout = 250;

		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			AppMetricId.ListenerTaskNotificationCount,
			AppMetricId.ListenerReconnectCount,
			AppMetricId.ListenerNotificationWaitTimeoutCount
		);

		private bool mIsDisposed = false;

		public PostgreSqlTaskQueueNotificationListener( string signalingConnectionString, string newTaskNotificationChannelName )
		{
			if ( string.IsNullOrEmpty( signalingConnectionString ) )
				throw new ArgumentNullException( nameof( signalingConnectionString ) );

			if ( string.IsNullOrEmpty( newTaskNotificationChannelName ) )
				throw new ArgumentNullException( nameof( newTaskNotificationChannelName ) );

			mSignalingConnectionString = signalingConnectionString;
			mNewTaskNotificationChannelName = newTaskNotificationChannelName;
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueueNotificationListener ),
					"Cannot reuse a disposed task queue notification listener" );
		}

		private void HandleTaskUpdateReceived( object sender, NpgsqlNotificationEventArgs e )
		{
			IncrementListenerTaskNotificationCount();

			EventHandler<NewTaskPostedEventArgs> eventHandler = NewTaskPosted;
			if ( eventHandler != null )
				eventHandler( this, new NewTaskPostedEventArgs() );
		}

		private void IncrementListenerTaskNotificationCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerTaskNotificationCount,
				m => m.Increment() );
		}

		private async Task ListenForTaskUpdatesAsync()
		{
			CancellationToken stopToken = mStopTokenSource.Token;
			if ( stopToken.IsCancellationRequested )
				return;

			try
			{
				await RunTaskUpdatesListeningLoopAsync( stopToken );
			}
			catch ( OperationCanceledException )
			{
				mLogger.Debug( "Stop requested. Listening loop has ended." );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Error occured while listening for new task notifications.", exc );
				throw;
			}
		}

		private async Task RunTaskUpdatesListeningLoopAsync( CancellationToken stopToken )
		{
			while ( true )
				await RunTaskUpdatesListeningIterationAsync( stopToken );
		}

		private async Task RunTaskUpdatesListeningIterationAsync( CancellationToken stopToken )
		{
			try
			{
				stopToken.ThrowIfCancellationRequested();
				//Signaling connection is not open:
				//  either is the first time we are connecting
				//  or the connection failed and we must attempt a reconnect
				if ( mSignalingConn == null )
					mSignalingConn = await InitiateSignalingConnectionAsync();

				if ( IsFirstConnectionAttempt )
					ProcessListenerConnected( mSignalingConn.ProcessID );
				else
					ProcessListenerConnectionRestored( mSignalingConn.ProcessID );

				///Start notification wait loop
				WaitForNotifications( mSignalingConn, stopToken );
			}
			catch ( NullReferenceException exc )
			{
				//Npgsql's Wait() may throw a NullReferenceException when the connection breaks
				mLogger.Error( "Possible connection failure while waiting", exc );
			}
			catch ( NpgsqlException exc )
			{
				//Catch and log database connection error so we can cleanup and re-connect;
				//  every other exception will flow to the caller's catch block and will break the loop
				mLogger.Error( "Database error detected while listening for new task notifications.", exc );
			}
			finally
			{
				//Clean-up database connection here. 
				//  This is reached when either the connection is lost, 
				//  so we need to clean-up before restoring
				//  or some other condition occurs (such as cancellation or other error)
				//  so we need to clean-up before exiting
				await CleanupSignalingConnectionAsync( mSignalingConn );
				mSignalingConn = null;
			}
		}

		private async Task<NpgsqlConnection> InitiateSignalingConnectionAsync()
		{
			mLogger.Debug( "Attempting to open signaling connection..." );

			NpgsqlConnection signalingConn = new NpgsqlConnection( mSignalingConnectionString );
			await signalingConn.OpenAsync();
			await signalingConn.ListenAsync( mNewTaskNotificationChannelName, HandleTaskUpdateReceived );

			mLogger.Debug( "Signaling connection successfully open." );

			return signalingConn;
		}

		private void ProcessListenerConnected( int connectionProcessId )
		{
			mWaitForFirstStartWaitHandle.Set();
			UpdateListenerConnectionBackendProcessId( connectionProcessId );

			EventHandler<ListenerConnectedEventArgs> eventHandler = ListenerConnected;
			if ( eventHandler != null )
				eventHandler( this, new ListenerConnectedEventArgs() );
		}

		private void UpdateListenerConnectionBackendProcessId( int connectionProcessId )
		{
			mListenerConnectionBackendProcessId = connectionProcessId;
		}

		private void ProcessListenerConnectionRestored( int connectionProcessId )
		{
			IncrementListenerReconnectCount();
			UpdateListenerConnectionBackendProcessId( connectionProcessId );

			EventHandler<ListenerConnectionRestoredEventArgs> eventHandler = ListenerConnectionRestored;
			if ( eventHandler != null )
				eventHandler( this, new ListenerConnectionRestoredEventArgs() );
		}

		private void IncrementListenerReconnectCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerReconnectCount,
				m => m.Increment() );
		}

		private async Task CleanupSignalingConnectionAsync( NpgsqlConnection signalingConn )
		{
			if ( signalingConn == null )
			{
				mLogger.Debug( "Connection was null. Nothing to perform." );
				return;
			}

			mLogger.DebugFormat( "Signaling connection state is {0}.",
				signalingConn.FullState.ToString() );

			if ( signalingConn.IsListening( mNewTaskNotificationChannelName ) )
				await signalingConn.UnlistenAsync( mNewTaskNotificationChannelName, HandleTaskUpdateReceived );

			if ( signalingConn.IsConnectionSomewhatOpen() )
				await signalingConn.CloseAsync();

			signalingConn.Dispose();
		}

		private void WaitForNotifications( NpgsqlConnection signalingConn, CancellationToken stopToken )
		{
			//At this point, check if cancellation was requested 
			//  and exit if so
			stopToken.ThrowIfCancellationRequested();
			while ( true )
			{
				mLogger.DebugFormat( "Waiting for notifications on channel {0}...",
					mNewTaskNotificationChannelName );

				bool hadNotification = signalingConn.Wait( mWaitNotificationTimeout );
				if ( !hadNotification )
				{
					ProcessListenerTimedOutWhileWaiting();
					mLogger.Debug( "Listener timed out while waiting. Checking stop token and restarting wait..." );
				}
				else
					mLogger.Debug( "Task Notification received." );

				//At this point a notification has been received:
				//   before waiting again, 
				//  check if cancellation was requested
				stopToken.ThrowIfCancellationRequested();
			}
		}

		private void ProcessListenerTimedOutWhileWaiting()
		{
			IncrementListenerNotificationWaitTimeoutCount();

			EventHandler<ListenerTimedOutEventArgs> eventHandler = ListenerTimedOutWhileWaiting;
			if ( eventHandler != null )
				eventHandler( this, new ListenerTimedOutEventArgs() );
		}

		private void IncrementListenerNotificationWaitTimeoutCount()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerNotificationWaitTimeoutCount,
				m => m.Increment() );
		}

		public async Task StartAsync()
		{
			CheckNotDisposedOrThrow();

			mLogger.Debug( "Received request to start listening for queue notifications." );
			if ( IsStopped )
				await TryRequestStartAsync();
			else
				mLogger.Debug( "Notification listener is not stopped. Nothing to do." );
		}

		private async Task TryRequestStartAsync()
		{
			await mStateController.TryRequestStartAsync( async ()
				=> await DoStartupSequenceAsync() );
		}

		private async Task DoStartupSequenceAsync()
		{
			mLogger.DebugFormat( "Starting notification listener for channel {0}...",
				mNewTaskNotificationChannelName );

			ResetSynchronization();
			await StartListeningForTaskUpdatesAsync();

			mLogger.DebugFormat( "Successfully started notification listener for channel {0}.",
				mNewTaskNotificationChannelName );
		}

		private void ResetSynchronization()
		{
			mWaitForFirstStartWaitHandle.Reset();
			mListenerConnectionBackendProcessId = 0;
		}

		private async Task StartListeningForTaskUpdatesAsync()
		{
			mStopTokenSource = new CancellationTokenSource();
			mNewTaskUpdatesListenerWorker = Task.Run( async () => await ListenForTaskUpdatesAsync() );
			await mWaitForFirstStartWaitHandle.ToTask();
		}

		public async Task StopAsync()
		{
			CheckNotDisposedOrThrow();

			mLogger.Debug( "Received request to stop listening for queue notifications." );
			if ( IsStarted )
				await TryRequestStopAsync();
			else
				mLogger.Debug( "Notification listener is not started. Nothing to do." );
		}

		private async Task TryRequestStopAsync()
		{
			await mStateController.TryRequestStopASync( async ()
				=> await DoShutdownSequenceAsync() );
		}

		private async Task DoShutdownSequenceAsync()
		{
			mLogger.DebugFormat( "Stopping notification listener for channel {0}...",
				mNewTaskNotificationChannelName );

			try
			{
				RequestTaskUpdatesListeningCancellation();
				await WaitForTaskUpdatesListeningShutdownAsync();
			}
			finally
			{
				ResetSynchronization();
				CleanupTaskUpdatesListening();
			}

			mLogger.DebugFormat( "Successfully stopped notification listener for channel {0}.",
				mNewTaskNotificationChannelName );
		}

		private void RequestTaskUpdatesListeningCancellation()
		{
			mStopTokenSource.Cancel();
		}

		private async Task WaitForTaskUpdatesListeningShutdownAsync()
		{
			await mNewTaskUpdatesListenerWorker;
		}

		private void CleanupTaskUpdatesListening()
		{
			mStopTokenSource?.Dispose();
			mStopTokenSource = null;
			mNewTaskUpdatesListenerWorker = null;
		}

		public void Dispose()
		{
			Dispose( false );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAsync().Wait();
					CleanupTaskUpdatesListeningSynchronization();
				}

				mIsDisposed = true;
			}
		}

		private void CleanupTaskUpdatesListeningSynchronization()
		{
			mWaitForFirstStartWaitHandle?.Dispose();
			mWaitForFirstStartWaitHandle = null;
		}

		public AppMetric QueryMetric( AppMetricId metricId )
		{
			return mMetrics.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetrics.CollectMetrics();
		}

		private bool IsFirstConnectionAttempt
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mListenerConnectionBackendProcessId <= 0;
			}
		}

		public bool IsStarted
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}

		private bool IsStopped
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStopped;
			}
		}

		public int ListenerConnectionBackendProcessId
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mListenerConnectionBackendProcessId;
			}
		}

		public IEnumerable<AppMetricId> ExportedMetrics
		{
			get
			{
				return mMetrics.ExportedMetrics;
			}
		}
	}
}
