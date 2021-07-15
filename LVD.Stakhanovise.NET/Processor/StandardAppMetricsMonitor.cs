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
using LVD.Stakhanovise.NET.Logging;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardAppMetricsMonitor : IAppMetricsMonitor, IDisposable
	{
		private static readonly IStakhanoviseLogger mLogger = StakhanoviseLogManager
			.GetLogger( MethodBase
				.GetCurrentMethod()
				.DeclaringType );

		private bool mIsDisposed = false;

		private IAppMetricsMonitorWriter mWriter;

		private IEnumerable<IAppMetricsProvider> mProviders;

		private AppMetricsMonitorOptions mOptions;

		private Timer mMetricsProcessingTimer;

		private StateController mStateController =
			new StateController();

		public StandardAppMetricsMonitor( AppMetricsMonitorOptions options,
			IAppMetricsMonitorWriter writer )
		{
			mOptions = options
				?? throw new ArgumentNullException( nameof( options ) );
			mWriter = writer
				?? throw new ArgumentNullException( nameof( writer ) );
		}

		private void CheckNotDisposedOrThrow()
		{
			if ( mIsDisposed )
				throw new ObjectDisposedException( nameof( StandardExecutionPerformanceMonitor ),
					"Cannot reuse a disposed app metrics monitor" );
		}

		private void DoStartupSequence( IEnumerable<IAppMetricsProvider> providers )
		{
			mMetricsProcessingTimer = new Timer();
			mMetricsProcessingTimer.Interval = mOptions.CollectionIntervalMilliseconds;
			mMetricsProcessingTimer.AutoReset = false;
			mMetricsProcessingTimer.Elapsed += HandleMetricProcessingTimerElapsed;
			mMetricsProcessingTimer.Start();
		}

		private async void HandleMetricProcessingTimerElapsed( object sender, ElapsedEventArgs e )
		{
			if ( !mStateController.IsStarted )
				return;

			await CollectMetricsAsync();

			if ( mStateController.IsStarted )
				mMetricsProcessingTimer.Start();
		}

		private async Task CollectMetricsAsync()
		{
			IEnumerable<AppMetric> metrics = AppMetricsCollection
				.JoinCollectMetrics( mProviders.ToArray() );

			if ( metrics.Count() > 0 )
				await mWriter.WriteAsync( metrics );
		}

		public Task StartAsync( params IAppMetricsProvider[] providers )
		{
			CheckNotDisposedOrThrow();

			mProviders = providers
				?? throw new ArgumentNullException( nameof( providers ) );

			if ( mStateController.IsStopped )
				mStateController.TryRequestStart( () => DoStartupSequence( providers ) );
			else
				mLogger.Debug( "App metrics monitor is already stopped. Nothing to be done." );

			return Task.CompletedTask;
		}

		private async Task DoShutdownSequenceAsync()
		{
			await CollectMetricsAsync();
			Cleanup();
		}

		private void Cleanup()
		{
			mProviders = null;
			mMetricsProcessingTimer.Elapsed -= HandleMetricProcessingTimerElapsed;
			mMetricsProcessingTimer.Stop();
			mMetricsProcessingTimer.Dispose();
			mMetricsProcessingTimer = null;
		}

		public async Task StopAsync()
		{
			CheckNotDisposedOrThrow();

			if ( mStateController.IsStarted )
				await mStateController.TryRequestStopASync( async ()
					=> await DoShutdownSequenceAsync() );
			else
				mLogger.Debug( "App metrics monitor is already stopped. Nothing to be done." );
		}

		protected void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
				{
					StopAsync().Wait();
					mStateController = null;
					mWriter = null;
				}

				mIsDisposed = true;
			}
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		public bool IsRunning
		{
			get
			{
				CheckNotDisposedOrThrow();
				return mStateController.IsStarted;
			}
		}
	}
}
