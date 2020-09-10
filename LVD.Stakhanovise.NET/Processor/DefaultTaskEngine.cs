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
using log4net;
using LVD.Stakhanovise.NET.Executors;
using LVD.Stakhanovise.NET.Queue;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class DefaultTaskEngine : ITaskEngine
	{
		private static readonly ILog mLogger = LogManager.GetLogger( MethodBase
			.GetCurrentMethod()
			.DeclaringType );

		private ITaskQueueConsumer mTaskQueueConsumer;

		private ITaskBuffer mTaskBuffer;

		private ITaskPoller mTaskPoller;

		private ITaskExecutorRegistry mExecutorRegistry;

		private ITaskResultQueue mTaskResultQueue;

		private List<ITaskWorker> mWorkers = new List<ITaskWorker>();

		private StateController mStateController
			= new StateController();

		private IKernel mKernel;

		private bool mIsDisposed;

		private int mWorkerCount;

		public DefaultTaskEngine ( int workerCount,
			ITaskQueueConsumer taskQueueConsumer,
			IKernel kernel )
		{
			mTaskQueueConsumer = taskQueueConsumer
				?? throw new ArgumentNullException( nameof( taskQueueConsumer ) );
			mKernel = kernel
				?? throw new ArgumentNullException( nameof( kernel ) );

			if ( workerCount <= 0 )
				throw new ArgumentOutOfRangeException( "Worker count must be greater than 0", nameof( workerCount ) );

			mTaskBuffer = new DefaultTaskBuffer( mTaskQueueConsumer.DequeuePoolSize );
			mTaskPoller = new DefaultTaskPoller( mTaskQueueConsumer, mTaskBuffer );
			mTaskResultQueue = new DefaultTaskResultQueue( mTaskQueueConsumer );
			mExecutorRegistry = new DefaultTaskExecutorRegistry( ResolveExecutorDependency );
			mWorkerCount = workerCount;
		}

		private object ResolveExecutorDependency ( Type type )
		{
			return mKernel.TryGet( type );
		}

		private void CheckDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( DefaultTaskEngine ), "Cannot reuse a disposed task result queue" );
		}

		private string[] GetRequiredPayloadTypeNames ()
		{
			return mExecutorRegistry
				.DetectedPayloadTypes
				.Select( t => t.FullName )
				.ToArray() ?? new string[ 0 ];
		}

		public void ScanAssemblies ( params Assembly[] assemblies )
		{
			mLogger.Info( "Scanning given assemblies for task executors..." );
			mExecutorRegistry.ScanAssemblies( assemblies );
			mLogger.Info( "Done scanning given assemblies for task executors." );
		}

		public async Task StartAsync ()
		{
			CheckDisposedOrThrow();

			if ( !mStateController.IsStopped )
			{
				mLogger.Info( "The task engine is already started." );
				return;
			}

			string[] requiredPayloadTypes =
				GetRequiredPayloadTypeNames();

			mLogger.DebugFormat( "Attempting to start the task engine with {0} workers and payload types: {1}.",
				mWorkerCount,
				string.Join( ",", requiredPayloadTypes ) );

			await mStateController.TryRequestStartAsync( async () =>
			{
				//Start the task poller
				mLogger.Info( "Attempting to start the task poller..." );
				await mTaskPoller.StartAsync( requiredPayloadTypes );
				mLogger.Info( "The task poller has been successfully started. Attempting to start workers." );

				//Start workers
				mLogger.InfoFormat( "Attempting to start {0} workers...", mWorkerCount );
				for ( int i = 0; i < mWorkerCount; i++ )
				{
					ITaskWorker taskWorker = new DefaultTaskWorker( mTaskBuffer,
						mExecutorRegistry,
						mTaskResultQueue );

					await taskWorker.StartAsync( requiredPayloadTypes );
					mWorkers.Add( taskWorker );
				}

				mLogger.Info( "All the workers have been successfully started." );
			} );

			mLogger.Info( "The task engine has been successfully started." );
		}

		public async Task StopAync ()
		{
			CheckDisposedOrThrow();

			if ( !mStateController.IsStarted )
			{
				mLogger.Info( "The task engine is already stopped." );
				return;
			}

			mLogger.Info( "Attempting to stop the task engine." );

			await mStateController.TryRequestStopASync( async () =>
			{
				//Stop the task poller
				mLogger.Info( "Attempting to stop the task poller." );
				await mTaskPoller.StopAync();
				mLogger.Info( "The task poller has been successfully stopped. Attempting to stop workers." );

				//Stop all the workers
				foreach ( ITaskWorker worker in mWorkers )
					await TryStopWorker( worker );

				mWorkers.Clear();
				mLogger.Info( "All the workers have been successfully stopped." );
			} );

			mLogger.Info( "The task engine has been successfully stopped." );
		}

		private async Task TryStopWorker ( ITaskWorker worker )
		{
			using ( worker )
				await worker.StopAync();
		}

		protected void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAync().Wait();

					mTaskResultQueue.Dispose();
					mTaskPoller.Dispose();
					mTaskBuffer.Dispose();

					mTaskResultQueue = null;
					mTaskPoller = null;
					mTaskBuffer = null;

					mTaskQueueConsumer = null;
					mKernel = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public IEnumerable<ITaskWorker> Workers
			=> mWorkers.AsReadOnly();

		public ITaskPoller TaskPoller
			=> mTaskPoller;

		public bool IsRunning
		{
			get
			{
				CheckDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}
	}
}
