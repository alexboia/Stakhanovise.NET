using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class MethodExecutionHelpers
	{
		public static async Task<TimedExecutionResult<TResult>> ExecuteWithTimingAsync<TResult> ( this Func<Task<TResult>> timedFunction )
		{
			if ( timedFunction == null )
				throw new ArgumentNullException( nameof( timedFunction ) );
			
			MonotonicTimestamp start = MonotonicTimestamp
				.Now();
			
			TResult result = await timedFunction();
			
			MonotonicTimestamp end = MonotonicTimestamp
				.Now();

			return new TimedExecutionResult<TResult>( result, 
				duration: end - start );
		}
	}
}
