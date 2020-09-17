using LVD.Stakhanovise.NET.Helpers;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	/// <summary>
	/// Courtesy of https://antonymale.co.uk/monotonic-timestamps-in-csharp.html
	/// </summary>
	public class MonotonicTimestamp
	{
		private static double tickFrequency;

		private long timestamp;

		static MonotonicTimestamp ()
		{
			long frequency;
			bool succeeded = NativeMethods.QueryPerformanceFrequency( out frequency );

			if ( !succeeded )
				throw new PlatformNotSupportedException( "Requires Windows XP or later" );

			tickFrequency = ( double )TimeSpan.TicksPerSecond / frequency;
		}

		private MonotonicTimestamp ( long timestamp )
		{
			this.timestamp = timestamp;
		}

		public static MonotonicTimestamp Now ()
		{
			long value;
			NativeMethods.QueryPerformanceCounter( out value );
			return new MonotonicTimestamp( value );
		}

		public static TimeSpan operator - ( MonotonicTimestamp to, MonotonicTimestamp from )
		{
			if ( to.timestamp == 0 )
				throw new ArgumentException( "Must be created using MonotonicTimestamp.Now(), not default(MonotonicTimestamp)",
					nameof( to ) );

			if ( from.timestamp == 0 )
				throw new ArgumentException( "Must be created using MonotonicTimestamp.Now(), not default(MonotonicTimestamp)", 
					nameof( from ) );

			long ticks = unchecked(( long )( ( to.timestamp - from.timestamp ) * tickFrequency ));
			return new TimeSpan( ticks );
		}
	}
}
