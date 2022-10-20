using System.Threading;

namespace LVD.Stakhanovise.NET.Tests.Payloads
{
	public class CancellationObservedPayload
	{
		public CancellationObservedPayload()
		{
			SyncHandle = new ManualResetEvent( false );
		}

		public void SendCancellation( CancellationTokenSource usingStopSource )
		{
			usingStopSource.Cancel();
			SyncHandle.Set();
		}

		public ManualResetEvent SyncHandle
		{
			get; set;
		}
	}
}
