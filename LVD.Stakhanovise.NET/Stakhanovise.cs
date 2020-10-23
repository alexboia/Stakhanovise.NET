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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Setup;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET
{
	public sealed class Stakhanovise : IDisposable
	{
		private bool mIsDisposed;

		private StakhanoviseSetup mStakhanoviseSetup;


		private ITaskEngine mEngine;

		private IAppMetricsMonitor mAppMetricsMonitor;

		public Stakhanovise ()
		{
			int defaultWorkerCount = Math.Max( 1, Environment.ProcessorCount - 1 );

			//Define default values that will be fed to the setup API
			StakhanoviseSetupDefaults defaults = new StakhanoviseSetupDefaults()
			{
				Mapping = new QueuedTaskMapping(),
				ExecutorAssemblies = GetDefaultAssembliesToScan(),
				WorkerCount = defaultWorkerCount,

				CalculateDelayTicksTaskAfterFailure = token
					=> ( long )Math.Pow( 10, token.LastQueuedTaskResult.ErrorCount + 1 ),

				IsTaskErrorRecoverable = ( task, exc )
					=> !( exc is NullReferenceException )
						&& !( exc is ArgumentException ),

				FaultErrorThresholdCount = 5,

				AppMetricsCollectionIntervalMilliseconds = 10000,
				AppMetricsMonitoringEnabled = true
			};

			//Init setup API
			mStakhanoviseSetup = new StakhanoviseSetup( defaults );
		}

		private void CheckNotDisposedOrThrow ()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( Stakhanovise ),
					"Cannot reuse a Stakhanovise instance" );
		}

		private Assembly[] GetDefaultAssembliesToScan ()
		{
			return new Assembly[]
			{
				Assembly.GetExecutingAssembly()
			};
		}

		public static Stakhanovise CreateForTheMotherland ()
		{
			return new Stakhanovise();
		}

		public Stakhanovise SetupWorkingPeoplesCommittee ( Action<IStakhanoviseSetup> setupAction )
		{
			CheckNotDisposedOrThrow();

			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			setupAction.Invoke( mStakhanoviseSetup );
			return this;
		}

		public async Task<Stakhanovise> StartFulfillingFiveYearPlanAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mEngine == null )
				mEngine = mStakhanoviseSetup.BuildTaskEngine();

			if ( mAppMetricsMonitor == null )
				mAppMetricsMonitor = mStakhanoviseSetup.BuildAppMetricsMonitor();

			if ( !mEngine.IsRunning )
				await mEngine.StartAsync();

			if ( mAppMetricsMonitor != null
					&& !mAppMetricsMonitor.IsRunning
					&& mEngine is IAppMetricsProvider )
				await mAppMetricsMonitor.StartAsync( ( IAppMetricsProvider )mEngine );

			return this;
		}

		public async Task<Stakhanovise> StopFulfillingFiveYearPlanAsync ()
		{
			CheckNotDisposedOrThrow();

			if ( mEngine != null && mEngine.IsRunning )
				await mEngine.StopAync();

			if ( mAppMetricsMonitor != null && mAppMetricsMonitor.IsRunning )
				await mAppMetricsMonitor.StopAsync();

			return this;
		}

		private void Dispose ( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopFulfillingFiveYearPlanAsync().Wait();
					mEngine?.Dispose();
					mEngine = null;

					IDisposable disposableAppMetricsMonitor = mAppMetricsMonitor as IDisposable;
					if ( disposableAppMetricsMonitor != null )
						disposableAppMetricsMonitor.Dispose();
				}

				mIsDisposed = true;
			}
		}

		public void Dispose ()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}
	}
}
