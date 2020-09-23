using log4net;
using LVD.Stakhanovise.NET.Helpers;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Setup;
using SqlKata.Compilers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LVD.Stakhanovise.NET.Processor
{
	public class DefaultExecutionPerformanceMonitor : IExecutionPerformanceMonitor, IDisposable
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		private bool mIsDisposed = false;

		private Task mFlushTask = null;

		private ManualResetEvent mFlushThresholdCountHandle = null;

		private CancellationTokenSource mFlushStopTokenSource = null;

		private int mReportCount = 0;

		private StateController mFlushStateController =
			new StateController();

		private ConcurrentDictionary<Type, TaskExecutionStats> mExecutionTimeInfo =
			new ConcurrentDictionary<Type, TaskExecutionStats>();

		private IExecutionPerformanceMonitorWriter mFlushWriter;

		private ExecutionPerformanceMonitorWriteOptions mFlushOptions;

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( DefaultExecutionPerformanceMonitor ),
					"Cannot reuse a disposed execution performance monitor" );
		}

		public TaskExecutionStats GetExecutionTimeInfo ( Type payloadType )
		{
			if ( payloadType == null )
				throw new ArgumentNullException( nameof( payloadType ) );

			if ( !mExecutionTimeInfo.TryGetValue( payloadType, out TaskExecutionStats executionTimeInfo ) )
				executionTimeInfo = TaskExecutionStats.Zero();

			return executionTimeInfo;
		}

		public void ReportExecutionTime ( Type payloadType, long durationMilliseconds )
		{
			if ( payloadType == null )
				throw new ArgumentNullException( nameof( payloadType ) );

			mExecutionTimeInfo.AddOrUpdate( payloadType,
				addValueFactory: key => TaskExecutionStats.Initial( durationMilliseconds ),
				updateValueFactory: ( key, lastStats ) => lastStats.UpdateWithNewCycleExecutionTime( durationMilliseconds ) );

			if ( mFlushStateController.IsStarted )
			{
				int newReportCount = Interlocked.Increment( ref mReportCount );
				if ( newReportCount % mFlushOptions.WriteCountThreshold == 0 )
					mFlushThresholdCountHandle.Set();
			}
		}

		private async Task RunFlushLoopAsync ( CancellationToken stopToken )
		{
			while ( true )
			{
				try
				{
					stopToken.ThrowIfCancellationRequested();

					await mFlushThresholdCountHandle.ToTask( mFlushOptions.WriteIntervalThresholdMilliseconds );
					stopToken.ThrowIfCancellationRequested();

					await mFlushWriter.WriteAsync( ExecutionTimeInfo );
					stopToken.ThrowIfCancellationRequested();

					mFlushThresholdCountHandle.Reset();
				}
				catch ( OperationCanceledException )
				{
					break;
				}
				catch ( Exception exc )
				{
					mLogger.Error( "Error flushing execution time info", exc );
				}
			}
		}

		private async Task DoFlushingStartupSequenceAsync ( IExecutionPerformanceMonitorWriter writer,
			ExecutionPerformanceMonitorWriteOptions options )
		{
			mFlushWriter = writer;
			mFlushOptions = options;
			mFlushStopTokenSource = new CancellationTokenSource();
			mFlushThresholdCountHandle = new ManualResetEvent( initialState: false );

			await mFlushWriter.SetupIfNeededAsync();
			mFlushTask = Task.Run( async () => await RunFlushLoopAsync( mFlushStopTokenSource.Token ) );
		}

		public async Task StartFlushingAsync ( IExecutionPerformanceMonitorWriter writer,
			ExecutionPerformanceMonitorWriteOptions options )
		{
			CheckDisposedOrThrow();

			if ( mFlushStateController.IsStopped )
				await mFlushStateController.TryRequestStartAsync( async ()
					=> await DoFlushingStartupSequenceAsync( writer, options ) );
			else
				mLogger.Debug( "Flush scheduler is already started. Nothing to be done." );
		}

		private async Task DoFlushingShutdownSequenceAsync ()
		{
			mFlushStopTokenSource.Cancel();
			await mFlushTask;

			mFlushTask.Dispose();
			mFlushStopTokenSource.Dispose();
			mFlushThresholdCountHandle.Dispose();

			mFlushTask = null;
			mFlushStopTokenSource = null;
			mFlushThresholdCountHandle = null;
			mFlushOptions = null;
			mFlushWriter = null;
		}

		public async Task StopFlushingAsync ()
		{
			CheckDisposedOrThrow();

			if ( mFlushStateController.IsStarted )
				await mFlushStateController.TryRequestStartAsync( async ()
					=> await DoFlushingShutdownSequenceAsync() );
			else
				mLogger.Debug( "Flush scheduler is already stopped. Nothing to be done." );
		}

		protected void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopFlushingAsync().Wait();

					mExecutionTimeInfo.Clear();
					mExecutionTimeInfo = null;
					mFlushStateController = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public IReadOnlyDictionary<Type, TaskExecutionStats> ExecutionTimeInfo =>
			new ReadOnlyDictionary<Type, TaskExecutionStats>( mExecutionTimeInfo );
	}
}
