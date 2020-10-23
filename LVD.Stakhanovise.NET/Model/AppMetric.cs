using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LVD.Stakhanovise.NET.Model
{
	public class AppMetric : IEquatable<AppMetric>
	{
		private long mValue = 0;

		private AppMetricId mId;

		public AppMetric ( AppMetricId id )
			: this( id, 0 )
		{
			return;
		}

		public AppMetric ( AppMetricId id, long value )
		{
			mId = id ?? throw new ArgumentNullException( nameof( id ) );
			mValue = value;
		}

		public long Update ( long value )
		{
			return Interlocked.Exchange( ref mValue, value );
		}

		public long Add ( long amount )
		{
			return Interlocked.Add( ref mValue, amount );
		}

		public AppMetric JoinWith ( AppMetric other )
		{
			if ( other == null )
				throw new ArgumentNullException( nameof( other ) );

			if ( !Id.Equals( other.Id ) )
				throw new InvalidOperationException( "Cannot join with a metric that has a different ID" );

			AppMetric thisCopy = Copy();
			AppMetric otherCopy = other.Copy();

			return new AppMetric( thisCopy.Id, otherCopy.Value + thisCopy.Value );
		}

		public long Max ( long newValue )
		{
			long initialValue,
				previousValue;

			do
			{
				initialValue = Interlocked.Read( ref mValue );
				if ( initialValue >= newValue )
					return initialValue;
			}
			while ( ( previousValue = Interlocked.CompareExchange( ref mValue, newValue, initialValue ) )
				!= initialValue );

			return previousValue;
		}

		public long Min ( long newValue )
		{
			long initialValue,
				previousValue;

			do
			{
				initialValue = Interlocked.Read( ref mValue );
				if ( initialValue <= newValue )
					return initialValue;
			}
			while ( ( previousValue = Interlocked.CompareExchange( ref mValue, newValue, initialValue ) )
				!= initialValue );

			return previousValue;
		}

		public long Increment ()
		{
			return Interlocked.Increment( ref mValue );
		}

		public long Decrement ()
		{
			return Interlocked.Decrement( ref mValue );
		}

		public AppMetric Copy ()
		{
			return new AppMetric( Id, Interlocked.Read( ref mValue ) );
		}

		public bool Equals ( AppMetric other )
		{
			return other != null
				&& Id.Equals( other.Id )
				&& Value == other.Value;
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as AppMetric );
		}

		public override int GetHashCode ()
		{
			int result = 1;

			result = result * 13 + mId.GetHashCode();
			result = result * 13 + mValue.GetHashCode();

			return result;
		}

		public AppMetricId Id => mId;

		public long Value => mValue;
	}
}
