// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using LVD.Stakhanovise.NET.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueNotificationListener : ITaskQueueNotificationListener,
		IAppMetricsProvider,
		IDisposable
	{
		public event EventHandler<NewTaskPostedEventArgs> NewTaskPosted;

		public event EventHandler<ListenerConnectedEventArgs> ListenerConnected;

		public event EventHandler<ListenerConnectionRestoredEventArgs> ListenerConnectionRestored;

		public event EventHandler<ListenerTimedOutEventArgs> ListenerTimedOutWhileWaiting;

		private readonly TaskQueueListenerOptions mOptions;

		private readonly ITaskQueueNotificationListenerMetricsProvider mMetricsProvider;

		private readonly IStakhanoviseLogger mLogger;

		private CancellationTokenSource mStopCoordinator;

		private ManualResetEvent mWaitForFirstStartWaitHandle = new ManualResetEvent( false );

		private StateController mStateController = new StateController();

		private Task mNewTaskUpdatesListenerWorker;

		private NpgsqlConnection mSignalingConn = null;

		private int mListenerConnectionBackendProcessId;

		private bool mIsDisposed = false;

		public PostgreSqlTaskQueueNotificationListener( TaskQueueListenerOptions options,
			ITaskQueueNotificationListenerMetricsProvider metricsProvider,
			IStakhanoviseLogger logger )
		{
			if ( options == null )
				throw new ArgumentNullException( nameof( options ) );

			if ( metricsProvider == null )
				throw new ArgumentNullException( nameof( metricsProvider ) );

			if ( logger == null )
				throw new ArgumentNullException( nameof( logger ) );

			mOptions = options;
			mMetricsProvider = metricsProvider;
			mLogger = logger;
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( PostgreSqlTaskQueueNotificationListener ),
					"Cannot reuse a disposed task queue notification listener"
				);
			}
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
			mMetricsProvider.IncrementTaskNotificationCount();
		}

		public async Task StartAsync()
		{
			CheckNotDisposedOrThrow();

			mLogger.Debug( "Received request to start listening for queue notifications." );
			if ( IsStopped )
				await TryRequestStartAsync();
			else
				mLogger.Debug( "Listener is started. Nothing to do." );
		}

		private async Task TryRequestStartAsync()
		{
			await mStateController.TryRequestStartAsync( DoStartupSequenceAsync );
		}

		private async Task DoStartupSequenceAsync()
		{
			mLogger.DebugFormat( "Starting notification listener for channel {0}...",
				mOptions.NewTaskNotificationChannelName );

			ResetSynchronization();
			await StartListeningForTaskUpdatesAsync();

			mLogger.DebugFormat( "Successfully started notification listener for channel {0}.",
				mOptions.NewTaskNotificationChannelName );
		}

		private void ResetSynchronization()
		{
			mWaitForFirstStartWaitHandle.Reset();
			mListenerConnectionBackendProcessId = 0;
		}

		private async Task StartListeningForTaskUpdatesAsync()
		{
			mStopCoordinator = new CancellationTokenSource();
			mNewTaskUpdatesListenerWorker = Task.Run( ListenForTaskUpdatesAsync );
			await mWaitForFirstStartWaitHandle.ToTask();
		}

		private async Task ListenForTaskUpdatesAsync()
		{
			try
			{
				CancellationToken stopToken = mStopCoordinator.Token;
				while ( !stopToken.IsCancellationRequested )
					await RunTaskUpdatesListeningIterationAsync( stopToken );
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

		private async Task RunTaskUpdatesListeningIterationAsync( CancellationToken stopToken )
		{
			try
			{
				stopToken.ThrowIfCancellationRequested();

				//Signaling connection is not open:
				//  either is the first time we are connecting
				//  or the connection failed and we must attempt a reconnect
				int processId = await InitiateSignalingConnectionIfNeededAsync();
				if ( IsFirstConnectionAttempt )
					ProcessListenerConnected( processId );
				else
					ProcessListenerConnectionRestored( processId );

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

		private async Task<int> InitiateSignalingConnectionIfNeededAsync()
		{
			if ( mSignalingConn == null )
			{
				mLogger.Debug( "Attempting to open signaling connection..." );

				NpgsqlConnection signalingConn = new NpgsqlConnection( mOptions.SignalingConnectionString );
				await signalingConn.OpenAsync();
				await signalingConn.ListenAsync( mOptions.NewTaskNotificationChannelName, HandleTaskUpdateReceived );

				mLogger.Debug( "Signaling connection successfully open." );
				mSignalingConn = signalingConn;
			}

			return mSignalingConn.ProcessID;
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
			mMetricsProvider.IncrementReconnectCount();
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

			if ( signalingConn.IsListening( mOptions.NewTaskNotificationChannelName ) )
				await signalingConn.UnlistenAsync( mOptions.NewTaskNotificationChannelName, HandleTaskUpdateReceived );

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
					mOptions.NewTaskNotificationChannelName );

				bool hadNotification = signalingConn.Wait( mOptions.WaitNotificationTimeout );
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
			mMetricsProvider.IncrementNotificationWaitTimeoutCount();
		}

		public async Task StopAsync()
		{
			CheckNotDisposedOrThrow();

			mLogger.Debug( "Received request to stop listening for queue notifications." );
			if ( IsStarted )
				await TryRequestStopAsync();
			else
				mLogger.Debug( "Listener is stopped. Nothing to do." );
		}

		private async Task TryRequestStopAsync()
		{
			await mStateController.TryRequestStopAsync( DoShutdownSequenceAsync );
		}

		private async Task DoShutdownSequenceAsync()
		{
			mLogger.DebugFormat( "Stopping notification listener for channel {0}...",
				mOptions.NewTaskNotificationChannelName );

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
				mOptions.NewTaskNotificationChannelName );
		}

		private void RequestTaskUpdatesListeningCancellation()
		{
			mStopCoordinator.Cancel();
		}

		private async Task WaitForTaskUpdatesListeningShutdownAsync()
		{
			await mNewTaskUpdatesListenerWorker;
		}

		private void CleanupTaskUpdatesListening()
		{
			mStopCoordinator?.Dispose();
			mStopCoordinator = null;
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

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			return mMetricsProvider.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetricsProvider.CollectMetrics();
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

		public IEnumerable<IAppMetricId> ExportedMetrics
		{
			get
			{
				return mMetricsProvider.ExportedMetrics;
			}
		}
	}
}
