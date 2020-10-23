// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
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

		private bool mIsDisposed = false;

		private string mSignalingConnectionString;

		private string mNewTaskNotificationChannelName;

		private StateController mStateController =
			new StateController();

		private CancellationTokenSource mStopTokenSource;

		private ManualResetEvent mWaitForFirstStartWaitHandle =
			new ManualResetEvent( initialState: false );

		private Task mNewTaskUpdatesListenerTask;

		private int mListenerConnectionBackendProcessId;

		private AppMetricsCollection mMetrics = new AppMetricsCollection
		(
			AppMetricId.ListenerTaskNotificationCount,
			AppMetricId.ListenerReconnectCount,
			AppMetricId.ListenerNotificationWaitTimeoutCount
		);

		public PostgreSqlTaskQueueNotificationListener ( string signalingConnectionString, string newTaskNotificationChannelName )
		{
			if ( string.IsNullOrEmpty( signalingConnectionString ) )
				throw new ArgumentNullException( nameof( signalingConnectionString ) );

			if ( string.IsNullOrEmpty( newTaskNotificationChannelName ) )
				throw new ArgumentNullException( nameof( newTaskNotificationChannelName ) );

			mSignalingConnectionString = signalingConnectionString;
			mNewTaskNotificationChannelName = newTaskNotificationChannelName;
		}

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueueNotificationListener ),
					"Cannot reuse a disposed task queue notification listener" );
		}

		private void UpdateListenerConnectionBackendProcessId ( int connectionProcessId )
		{
			mListenerConnectionBackendProcessId = connectionProcessId;
		}

		private void IncrementListenerReconnectCount ()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerReconnectCount,
				m => m.Increment() );
		}

		private void IncrementListenerTaskNotificationCount ()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerTaskNotificationCount,
				m => m.Increment() );
		}

		private void IncrementListenerNotificationWaitTimeoutCount ()
		{
			mMetrics.UpdateMetric( AppMetricId.ListenerNotificationWaitTimeoutCount,
				m => m.Increment() );
		}

		private void HandleTaskUpdateReceived ( object sender, NpgsqlNotificationEventArgs e )
		{
			IncrementListenerTaskNotificationCount();

			EventHandler<NewTaskPostedEventArgs> eventHandler = NewTaskPosted;
			if ( eventHandler != null )
				eventHandler( this, new NewTaskPostedEventArgs() );
		}

		private void ProcessListenerConnectionRestored ( int connectionProcessId )
		{
			IncrementListenerReconnectCount();
			UpdateListenerConnectionBackendProcessId( connectionProcessId );

			EventHandler<ListenerConnectionRestoredEventArgs> eventHandler = ListenerConnectionRestored;
			if ( eventHandler != null )
				eventHandler( this, new ListenerConnectionRestoredEventArgs() );
		}

		private void ProcessListenerConnected ( int connectionProcessId )
		{
			mWaitForFirstStartWaitHandle.Set();
			UpdateListenerConnectionBackendProcessId( connectionProcessId );

			EventHandler<ListenerConnectedEventArgs> eventHandler = ListenerConnected;
			if ( eventHandler != null )
				eventHandler( this, new ListenerConnectedEventArgs() );
		}

		private void ProcessListenerTimedOutWhileWaiting ()
		{
			IncrementListenerNotificationWaitTimeoutCount();

			EventHandler<ListenerTimedOutEventArgs> eventHandler = ListenerTimedOutWhileWaiting;
			if ( eventHandler != null )
				eventHandler( this, new ListenerTimedOutEventArgs() );
		}

		private async Task<NpgsqlConnection> InitiateSignalingConnectionAsync ()
		{
			mLogger.Debug( "Attempting to open signaling connection..." );

			NpgsqlConnection signalingConn = new NpgsqlConnection( mSignalingConnectionString );
			await signalingConn.OpenAsync();
			await signalingConn.ListenAsync( mNewTaskNotificationChannelName, HandleTaskUpdateReceived );

			mLogger.Debug( "Signaling connection successfully open." );

			return signalingConn;
		}

		private async Task CleanupSignalingConnectionAsync ( NpgsqlConnection signalingConn )
		{
			if ( signalingConn != null )
			{
				mLogger.DebugFormat( "Signalling connection state is {0}",
					signalingConn.FullState.ToString() );

				if ( signalingConn.IsListening( mNewTaskNotificationChannelName ) )
					await signalingConn.UnlistenAsync( mNewTaskNotificationChannelName, HandleTaskUpdateReceived );

				if ( signalingConn.IsConnectionSomewhatOpen() )
					await signalingConn.CloseAsync();

				signalingConn.Dispose();
			}
			else
				mLogger.Debug( "Connection was null. Nothing to perform." );
		}

		private void WaitForNotifications ( NpgsqlConnection signalingConn, CancellationToken cancellationToken )
		{
			//At this point, check if cancellation was requested 
			//  and exit if so
			cancellationToken.ThrowIfCancellationRequested();
			while ( true )
			{
				mLogger.DebugFormat( "Waiting for notifications on channel {0}...",
					mNewTaskNotificationChannelName );

				//Pass the cancellation token to the WaitAsync 
				//  to be able to stop the listener when requested
				try
				{
					//TODO: timeout should be configurable
					bool hadNotification = signalingConn.Wait( 250 );
					if ( !hadNotification )
					{
						ProcessListenerTimedOutWhileWaiting();
						mLogger.Debug( "Listener timed out while waiting. Checking stop token and restarting wait..." );
					}
					else
						mLogger.Debug( "Task Notification received." );
				}
				catch ( NullReferenceException exc )
				{
					mLogger.Error( "Possible connection failure while waiting", exc );
					break;
				}

				//At this point a notification has been received:
				//   before the next go-around of WaitAsync, 
				//  check if cancellation was requested
				cancellationToken.ThrowIfCancellationRequested();
			}
		}

		private async Task ListenForTaskUpdates ()
		{
			NpgsqlConnection signalingConn = null;
			CancellationToken stopToken = mStopTokenSource
				.Token;

			if ( stopToken.IsCancellationRequested )
				return;

			try
			{
				while ( true )
				{
					try
					{
						stopToken.ThrowIfCancellationRequested();
						//Signaling connection is not open:
						//  either is the first time we are connecting
						//  or the connection failed and we must attempt a reconnect
						if ( signalingConn == null )
							signalingConn = await InitiateSignalingConnectionAsync();

						//If this is the first time we are connecting
						//  set the wait handle for the first connection 
						//  to signal the completion of the initial listener start-up;
						//  otherwise notify that the connection has been lost and subsequently restored.
						if ( mListenerConnectionBackendProcessId <= 0 )
							ProcessListenerConnected( signalingConn.ProcessID );
						else
							ProcessListenerConnectionRestored( signalingConn.ProcessID );

						///Start notification wait loop
						WaitForNotifications( signalingConn, stopToken );
					}
					catch ( NpgsqlException exc )
					{
						//Catch and log database connection error;
						//  every other exception will flow to the outer catch block
						mLogger.Error( "Database error detected while listening for new task notifications.", exc );
					}
					finally
					{
						//Clean-up database connection here. 
						//  This is reached when either the connection is lost, 
						//  so we need to clean-up before restoring
						//  or some other condition occurs (such as cancellation or other error)
						//  so we need to clean-up before exiting
						await CleanupSignalingConnectionAsync( signalingConn );
						signalingConn = null;
					}
				}
			}
			catch ( OperationCanceledException )
			{
				mLogger.Debug( "New task notification wait token cancelled." );
			}
			catch ( Exception exc )
			{
				mLogger.Error( "Error occured while listening for new task notifications.", exc );
				throw;
			}
		}

		private async Task DoStartupSequenceAsync ()
		{
			mLogger.DebugFormat( "Starting queue notification listener for channel {0}...",
				mNewTaskNotificationChannelName );

			//Reset wait handle and create cancellation token source
			mWaitForFirstStartWaitHandle.Reset();
			mStopTokenSource = new CancellationTokenSource();

			//Reset diagnostics and start the listener thread
			mListenerConnectionBackendProcessId = 0;
			mNewTaskUpdatesListenerTask = Task.Run( async ()
				=> await ListenForTaskUpdates() );

			//Wait for connection to be established
			await mWaitForFirstStartWaitHandle.ToTask();

			mLogger.DebugFormat( "Successfully started queue notification listener for channel {0}.",
				mNewTaskNotificationChannelName );
		}

		public async Task StartAsync ()
		{
			CheckDisposedOrThrow();

			mLogger.Debug( "Received request to start listening for queue notifications." );
			if ( mStateController.IsStopped )
				await mStateController.TryRequestStartAsync( async () =>
					await DoStartupSequenceAsync() );
			else
				mLogger.Debug( "Queue notification listener is not stopped. Nothing to do." );
		}

		private async Task DoShutdownSequenceAsync ()
		{
			mLogger.DebugFormat( "Stopping queue notification listener for channel {0}...",
				mNewTaskNotificationChannelName );

			try
			{
				//Request cancellation and wait 
				//  for the task to complete
				mStopTokenSource.Cancel();
				await mNewTaskUpdatesListenerTask;
			}
			finally
			{
				//Reset wait handle and diagnostics
				mWaitForFirstStartWaitHandle.Reset();
				mListenerConnectionBackendProcessId = 0;

				//Cleanup cancellation token source 
				//	and the listener task
				mStopTokenSource?.Dispose();
				mStopTokenSource = null;
				mNewTaskUpdatesListenerTask = null;
			}

			mLogger.DebugFormat( "Successfully stopped queue notification listener for channel {0}.",
				mNewTaskNotificationChannelName );
		}

		public async Task StopAsync ()
		{
			CheckDisposedOrThrow();

			mLogger.Debug( "Received request to stop listening for queue notifications." );
			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopASync( async () => 
					await DoShutdownSequenceAsync() );
			else
				mLogger.Debug( "Queue notification listener is not started. Nothing to do." );
		}

		public void Dispose ()
		{
			Dispose( false );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAsync().Wait();

					mWaitForFirstStartWaitHandle?.Dispose();
					mWaitForFirstStartWaitHandle = null;
				}

				mIsDisposed = true;
			}
		}

		public AppMetric QueryMetric ( AppMetricId metricId )
		{
			return mMetrics.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics ()
		{
			return mMetrics.CollectMetrics();
		}

		public bool IsStarted
		{
			get
			{
				CheckDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}

		public int ListenerConnectionBackendProcessId
		{
			get
			{
				CheckDisposedOrThrow();
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
