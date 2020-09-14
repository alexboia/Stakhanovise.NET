// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
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
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET
{
	public sealed class StateController
	{
		private const int StateStopped = 0;

		private const int StateStartRequested = 1;

		private const int StateStarted = 2;

		private const int StateStopRequested = 3;

		private int mState;

		public StateController ()
		{
			mState = StateStopped;
		}

		private bool TryRequestStart ()
		{
			return Interlocked.CompareExchange( ref mState,
				value: StateStartRequested,
				comparand: StateStopped ) == StateStopped;
		}

		public void TryRequestStart ( Action onStartFn )
		{
			if ( onStartFn == null )
				throw new ArgumentNullException( nameof( onStartFn ) );

			if ( TryRequestStart() )
			{
				try
				{
					onStartFn.Invoke();
					Interlocked.Exchange( ref mState, StateStarted );
				}
				catch ( Exception )
				{
					Interlocked.Exchange( ref mState, StateStopped );
					throw;
				}
			}
		}

		public async Task TryRequestStartAsync ( Func<Task> onStartFn )
		{
			if ( onStartFn == null )
				throw new ArgumentNullException( nameof( onStartFn ) );

			if ( TryRequestStart() )
			{
				try
				{
					await onStartFn.Invoke();
					Interlocked.Exchange( ref mState, StateStarted );
				}
				catch ( Exception )
				{
					Interlocked.Exchange( ref mState, StateStopped );
					throw;
				}
			}
		}

		private bool TryRequestStop ()
		{
			return Interlocked.CompareExchange( ref mState,
				value: StateStopRequested,
				comparand: StateStarted ) == StateStarted;
		}

		public void TryRequestStop ( Action onStopFn )
		{
			if ( onStopFn == null )
				throw new ArgumentNullException( nameof( onStopFn ) );

			if ( TryRequestStop() )
			{
				try
				{
					onStopFn.Invoke();
					Interlocked.Exchange( ref mState, StateStopped );
				}
				catch ( Exception )
				{
					Interlocked.Exchange( ref mState, StateStarted );
					throw;
				}
			}
		}

		public async Task TryRequestStopASync ( Func<Task> onStopFn )
		{
			if ( onStopFn == null )
				throw new ArgumentNullException( nameof( onStopFn ) );

			if ( TryRequestStop() )
			{
				try
				{
					await onStopFn.Invoke();
					Interlocked.Exchange( ref mState, StateStopped );
				}
				catch ( Exception exc )
				{
					Interlocked.Exchange( ref mState, StateStarted );
					throw;
				}
			}
		}

		public void Reset ()
		{
			Interlocked.Exchange( ref mState, StateStopped );
		}

		public bool IsStartRequested => mState == StateStartRequested;

		public bool IsStopRequested => mState == StateStopRequested;

		public bool IsStarted => mState == StateStarted;

		public bool IsStopped => mState == StateStopped;
	}
}
