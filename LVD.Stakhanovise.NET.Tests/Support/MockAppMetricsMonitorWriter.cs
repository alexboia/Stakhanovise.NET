using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class MockAppMetricsMonitorWriter : IAppMetricsMonitorWriter, IDisposable
	{
		private readonly CountdownEvent mWriteCountLock;

		private readonly ConcurrentDictionary<string, List<IEnumerable<AppMetric>>> mWrittenMetrics =
			new ConcurrentDictionary<string, List<IEnumerable<AppMetric>>>();

		public MockAppMetricsMonitorWriter( int unlockAfterWriteCount )
		{
			mWriteCountLock = new CountdownEvent( unlockAfterWriteCount );
		}

		public Task<int> WriteAsync( string processId, IEnumerable<AppMetric> appMetrics )
		{
			if ( !mWrittenMetrics.TryGetValue( processId, out List<IEnumerable<AppMetric>> batches ) )
			{
				batches = new List<IEnumerable<AppMetric>>();
				mWrittenMetrics [ processId ] = batches;
			}

			batches.Add( appMetrics );

			if ( !mWriteCountLock.IsSet )
				mWriteCountLock.Signal();

			return Task.FromResult( 1 );
		}

		public List<IEnumerable<AppMetric>> GetWrittenBatchesForProcessId( string processId )
		{
			if ( !mWrittenMetrics.TryGetValue( processId, out List<IEnumerable<AppMetric>> batches ) )
				batches = new List<IEnumerable<AppMetric>>();

			return batches;
		}

		public int GetWrittenBatchCountForProcessId( string processId )
		{
			return GetWrittenBatchesForProcessId( processId ).Count();
		}

		public void ResetWriteCountLock( int unlockAfterWriteCount )
		{
			mWriteCountLock.Reset( unlockAfterWriteCount );
		}

		public void WaitForWriteCount()
		{
			mWriteCountLock.Wait();
		}

		public bool WaitForWriteCount( int millisecondsTimeout )
		{
			return mWriteCountLock.Wait( TimeSpan.FromMilliseconds( millisecondsTimeout ) );
		}

		public bool WaitForWriteCount( TimeSpan timeout )
		{
			return mWriteCountLock.Wait( timeout );
		}

		public void Dispose()
		{
			Dispose( true );
		}

		protected void Dispose( bool disposing )
		{
			if ( disposing )
			{
				mWriteCountLock.Dispose();
			}
		}
	}
}
