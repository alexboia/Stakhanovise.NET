using System.Threading;

namespace LVD.Stakhanovise.NET.Tests.Payloads
{
	public class WaitHandlePayload
	{
		public WaitHandlePayload()
			: this( new ManualResetEvent( false ) )
		{
			return;
		}

		public WaitHandlePayload( ManualResetEvent waitHandle )
		{
			WaitHandle = waitHandle;
		}

		public ManualResetEvent WaitHandle
		{
			get; set;
		}
	}
}
