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
