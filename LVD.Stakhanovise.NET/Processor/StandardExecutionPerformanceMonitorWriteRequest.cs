using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardExecutionPerformanceMonitorWriteRequest : AsyncProcessingRequest<int>
	{
		public StandardExecutionPerformanceMonitorWriteRequest ( long requestId,
			string payloadType,
			long durationMilliseconds,
			TaskCompletionSource<int> completionToken,
			int timeoutMilliseconds,
			int maxFailCount )
			: base( requestId, completionToken, timeoutMilliseconds, maxFailCount )
		{
			if ( string.IsNullOrEmpty( payloadType ) )
				throw new ArgumentNullException( nameof( payloadType ) );

			PayloadType = payloadType;
			DurationMilliseconds = durationMilliseconds;
		}

		public string PayloadType { get; private set; }

		public long DurationMilliseconds { get; private set; }
	}
}
