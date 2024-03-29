﻿// 
// BSD 3-Clause License
// 
// Copyright (c) 2020-2022, Boia Alexandru
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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StandardTaskBuffer : ITaskBuffer, IAppMetricsProvider
	{
		public event EventHandler QueuedTaskRetrieved;

		public event EventHandler QueuedTaskAdded;

		private readonly int mCapacity;

		private readonly ITaskBufferMetricsProvider mMetricsProvider;

		private BlockingCollection<IQueuedTaskToken> mInnerBuffer;

		private bool mIsDisposed = false;

		public StandardTaskBuffer( int capacity, ITaskBufferMetricsProvider metricsProvider )
		{
			if ( capacity <= 0 )
				throw new ArgumentOutOfRangeException( nameof( capacity ), "The capacity must be greater than 0" );

			if ( metricsProvider == null )
				throw new ArgumentNullException( nameof( metricsProvider ) );

			mInnerBuffer = new BlockingCollection<IQueuedTaskToken>( new ConcurrentQueue<IQueuedTaskToken>(), capacity );
			mCapacity = capacity;
			mMetricsProvider = metricsProvider;
		}

		public void BeginAdding()
		{
			CheckDisposedOrThrow();
			if ( mInnerBuffer.IsAddingCompleted )
				mInnerBuffer = new BlockingCollection<IQueuedTaskToken>();
		}

		private void CheckDisposedOrThrow()
		{
			if ( mIsDisposed )
			{
				throw new ObjectDisposedException(
					nameof( StandardTaskBuffer ),
					"Cannot reuse a disposed task buffer"
				);
			}
		}

		public bool TryAddNewTask( IQueuedTaskToken task )
		{
			CheckDisposedOrThrow();

			if ( task == null )
				throw new ArgumentNullException( nameof( task ) );

			if ( mInnerBuffer.IsAddingCompleted )
				return false;

			int oldCount = mInnerBuffer.Count;
			bool wasFull = ( oldCount == mCapacity );
			bool isAdded = mInnerBuffer.TryAdd( task );

			if ( isAdded )
			{
				NotifyQueuedTaskAdded();
				UpdateOnBufferItemAdd( Math.Min( mCapacity, oldCount + 1 ), wasFull );
			}

			return isAdded;
		}

		private void NotifyQueuedTaskAdded()
		{
			EventHandler itemAddedHandler = QueuedTaskAdded;
			if ( itemAddedHandler != null )
				itemAddedHandler.Invoke( this, EventArgs.Empty );
		}

		private void UpdateOnBufferItemAdd( int newCount, bool wasFull )
		{
			bool isFull = ( newCount == mCapacity );

			UpdateBufferCountStats( newCount );
			if ( !wasFull && isFull )
				IncrementTimesFilled();
		}

		private void UpdateBufferCountStats( int newCount )
		{
			mMetricsProvider.UpdateBufferCountStats( newCount );
		}

		private void IncrementTimesFilled()
		{
			mMetricsProvider.IncrementTimesFilled();
		}

		private void IncrementTimesEmptied()
		{
			mMetricsProvider.IncrementTimesEmptied();
		}

		public IQueuedTaskToken TryGetNextTask()
		{
			CheckDisposedOrThrow();

			int oldCount = mInnerBuffer.Count;
			bool wasEmpty = ( oldCount == 0 );

			if ( !mInnerBuffer.TryTake( out IQueuedTaskToken newTaskToken ) )
				newTaskToken = null;

			if ( newTaskToken != null )
			{
				NotifyQueuedTaskRetrieved();
				UpdateOnBufferItemRemove( Math.Max( 0, oldCount - 1 ), wasEmpty );
			}

			return newTaskToken;
		}

		private void NotifyQueuedTaskRetrieved()
		{
			EventHandler itemRetrievedHandler = QueuedTaskRetrieved;
			if ( itemRetrievedHandler != null )
				itemRetrievedHandler.Invoke( this, EventArgs.Empty );
		}

		private void UpdateOnBufferItemRemove( int newCount, bool wasEmpty )
		{
			bool isEmpty = newCount == 0;

			UpdateBufferCountStats( newCount );
			if ( !wasEmpty && isEmpty )
				IncrementTimesEmptied();
		}

		public void CompleteAdding()
		{
			CheckDisposedOrThrow();
			mInnerBuffer.CompleteAdding();
		}

		public AppMetric QueryMetric( IAppMetricId metricId )
		{
			return mMetricsProvider.QueryMetric( metricId );
		}

		public IEnumerable<AppMetric> CollectMetrics()
		{
			return mMetricsProvider.CollectMetrics();
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		protected virtual void Dispose( bool disposing )
		{
			if ( !mIsDisposed )
			{
				if ( disposing )
					mInnerBuffer.Dispose();

				mIsDisposed = true;
			}
		}

		public int Count
			=> mInnerBuffer.Count;

		public bool HasTasks
			=> mInnerBuffer.Count > 0;

		public bool IsEmpty
			=> !HasTasks;

		public bool IsFull
			=> mInnerBuffer.Count == mCapacity;

		public int Capacity
			=> mCapacity;

		public bool IsCompleted
			=> mInnerBuffer.IsCompleted;

		public IEnumerable<IAppMetricId> ExportedMetrics
			=> mMetricsProvider.ExportedMetrics;
	}
}
