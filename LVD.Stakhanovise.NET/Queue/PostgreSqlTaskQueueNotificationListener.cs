using log4net;
using LVD.Stakhanovise.NET.Helpers;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class PostgreSqlTaskQueueNotificationListener : IDisposable
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		public event EventHandler<NewTaskPostedEventArgs> NewTaskPosted;

		public event EventHandler<ListenerConnectionRestoredEventArgs> ListenerConnectionRestored;

		private bool mIsDisposed = false;

		private string mSignalingConnectionString;

		private string mNewTaskNotificationChannelName;

		private StateController mNewTaskUpdatesStateController =
			new StateController();

		private CancellationTokenSource mWaitForTaskUpdatesCancellationTokenSource;

		private ManualResetEvent mWaitForFirstStartWaitHandle =
			new ManualResetEvent( initialState: false );

		private Task mNewTaskUpdatesListenerTask;

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
				throw new ObjectDisposedException( nameof( PostgreSqlTaskQueueNotificationListener ), "Cannot reuse a disposed task queue notification listener" );
		}

		private async Task<NpgsqlConnection> OpenSignalingConnectionAsync ()
		{
			NpgsqlConnection connection = new NpgsqlConnection( mSignalingConnectionString );
			await connection.OpenAsync();
			return connection;
		}

		private void HandleNewTaskUpdateReceived ( object sender, NpgsqlNotificationEventArgs e )
		{
			NotifyNewTaskPosted();
		}

		private void NotifyNewTaskPosted ()
		{
			EventHandler<NewTaskPostedEventArgs> eventHandler = NewTaskPosted;
			if ( eventHandler != null )
				eventHandler( this, new NewTaskPostedEventArgs() );
		}

		private void NotifyListenerConnectionRestored ()
		{
			EventHandler<ListenerConnectionRestoredEventArgs> eventHandler = ListenerConnectionRestored;
			if ( eventHandler != null )
				eventHandler( this, new ListenerConnectionRestoredEventArgs() );
		}

		private async Task ListenForNewTaskUpdatesAsync ()
		{
			bool isFirstConnect = true;
			NpgsqlConnection signalingConn = null;
			CancellationToken token = mWaitForTaskUpdatesCancellationTokenSource.Token;

			try
			{
				while ( true )
				{
					try
					{
						token.ThrowIfCancellationRequested();
						//Signaling connection is not open:
						//  either is the first time we are connecting
						//  or the connection failed and we must attempt a reconnect
						if ( signalingConn == null )
						{
							mLogger.Debug( "Attempting to open signaling connection..." );

							signalingConn = await OpenSignalingConnectionAsync();
							await signalingConn.ListenAsync( mNewTaskNotificationChannelName,
								HandleNewTaskUpdateReceived );

							mLogger.Debug( "Signaling connection successfully open." );
						}

						//If this is the first time we are connecting
						//  set the wait handle for the first connection 
						//  to signal the completion of the initial listener start-up;
						//  otherwise notify that the connection has been lost and subsequently restored.
						if ( isFirstConnect )
						{
							mWaitForFirstStartWaitHandle.Set();
							isFirstConnect = false;
						}
						else
							NotifyListenerConnectionRestored();

						//At this point, check if cancellation was requested 
						//  and exit if so
						token.ThrowIfCancellationRequested();
						while ( true )
						{
							mLogger.DebugFormat( "Waiting for notifications on channel {0}...",
								mNewTaskNotificationChannelName );

							//Pass the cancellation token to the WaitAsync 
							//  to be able to stop the listener when requested
							await signalingConn.WaitAsync( mWaitForTaskUpdatesCancellationTokenSource.Token );

							mLogger.DebugFormat( "Notification received on channel {0}...",
								mNewTaskNotificationChannelName );

							//At this point a notification has been received:
							//   before the next go-around of WaitAsync, 
							//  check if cancellation was requested
							token.ThrowIfCancellationRequested();
						}
					}
					catch ( NpgsqlException exc )
					{
						//Catch and log database connection error;
						//  every other exception will flow to the outer catch block
						mLogger.Error( "Database error detected while listening for new task notifications.", exc );
					}
					finally
					{
						mLogger.Debug( "Cleaning-up signaling connection..." );
						//Clean-up database connection here. 
						//  This is reached when either the connection is lost, 
						//  so we need to clean-up before restoring
						//  or some other condition occurs (such as cancellation or other error)
						//  so we need to clean-up before exiting
						if ( signalingConn != null )
						{
							if ( signalingConn.IsListening( mNewTaskNotificationChannelName ) )
								await signalingConn.UnlistenAsync( mNewTaskNotificationChannelName, HandleNewTaskUpdateReceived );

							if ( signalingConn.IsConnectionSomewhatOpen() )
								signalingConn.Close();

							signalingConn.Dispose();
							signalingConn = null;
						}
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

		public async Task StartAsync ()
		{
			CheckDisposedOrThrow();

			mLogger.Debug( "Received request to start listening for queue notifications." );
			if ( mNewTaskUpdatesStateController.IsStopped )
			{
				await mNewTaskUpdatesStateController.TryRequestStartAsync( async () =>
				{
					mLogger.DebugFormat( "Starting queue notification listener for channel {0}...",
						mNewTaskNotificationChannelName );

					mWaitForFirstStartWaitHandle.Reset();
					mWaitForTaskUpdatesCancellationTokenSource = new CancellationTokenSource();

					mNewTaskUpdatesListenerTask = Task.Run( ListenForNewTaskUpdatesAsync );
					await mWaitForFirstStartWaitHandle.ToTask();

					mLogger.DebugFormat( "Successfully started queue notification listener for channel {0}.",
						mNewTaskNotificationChannelName );
				} );
			}
			else
				mLogger.Debug( "Queue notification listener is not stopped. Nothing to do." );
		}

		public async Task StopAsync ()
		{
			CheckDisposedOrThrow();

			mLogger.Debug( "Received request to stop listening for queue notifications." );
			if ( mNewTaskUpdatesStateController.IsStarted )
			{
				await mNewTaskUpdatesStateController.TryRequestStopASync( async () =>
				{
					mLogger.DebugFormat( "Stopping queue notification listener for channel {0}...",
						mNewTaskNotificationChannelName );

					try
					{
						//Request cancellation and wait 
						//  for the task to complete
						mWaitForTaskUpdatesCancellationTokenSource.Cancel();
						await mNewTaskUpdatesListenerTask;
					}
					finally
					{
						mWaitForFirstStartWaitHandle.Reset();
						mWaitForTaskUpdatesCancellationTokenSource?.Dispose();
						mWaitForTaskUpdatesCancellationTokenSource = null;
						mNewTaskUpdatesListenerTask = null;
					}

					mLogger.DebugFormat( "Successfully stopped queue notification listener for channel {0}.",
						mNewTaskNotificationChannelName );
				} );
			}
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

					if ( mWaitForFirstStartWaitHandle != null )
						mWaitForFirstStartWaitHandle.Dispose();

					mWaitForFirstStartWaitHandle = null;
				}

				mIsDisposed = true;
			}
		}

		public bool IsStarted => mNewTaskUpdatesStateController.IsStarted;
	}
}
