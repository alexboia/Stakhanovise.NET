using LVD.Stakhanovise.NET.Tests.Support;

namespace LVD.Stakhanovise.NET.Tests.AppMetricsTests
{
	public static class MockAppMetricsProviderExtensions
	{
		public static bool WaitForWriteCount( this MockAppMetricsMonitorWriter writer, int nCycles, int collectionIntervalMilliseconds )
		{
			int waitTimeout = ComputeWaitForWriteCountTimeout( nCycles, collectionIntervalMilliseconds );
			bool writeCountEventOccurred = writer.WaitForWriteCount( waitTimeout );
			return writeCountEventOccurred;
		}

		private static int ComputeWaitForWriteCountTimeout( int nCycles, int collectionIntervalMilliseconds )
		{
			return nCycles * collectionIntervalMilliseconds * 10;
		}
	}
}
