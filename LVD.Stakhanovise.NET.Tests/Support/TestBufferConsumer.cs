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
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TestBufferConsumer : IDisposable
	{
		private ITaskBuffer mTaskBuffer;

		List<IQueuedTaskToken> mConsumedTasks = new List<IQueuedTaskToken>();

		private Task mConsumeBufferTask;

		public TestBufferConsumer ( ITaskBuffer taskBuffer )
		{
			mTaskBuffer = taskBuffer;
		}

		public void StartConsumingBuffer ()
		{
			mConsumeBufferTask = Task.Run( () =>
			{
				while ( !mTaskBuffer.IsCompleted )
				{
					IQueuedTaskToken queuedTaskToken = mTaskBuffer.TryGetNextTask();
					if ( queuedTaskToken != null )
						mConsumedTasks.Add( queuedTaskToken );
					else
						Task.Delay( 10 ).Wait();
				}
			} );
		}

		public void WaitForBufferToBeConsumed ()
		{
			if ( mConsumeBufferTask == null )
				return;

			mConsumeBufferTask.Wait();
		}

		public void AssertMatchesProducedTasks ( IEnumerable<IQueuedTaskToken> producedTasks )
		{
			Assert.AreEqual( producedTasks.Count(),
				mConsumedTasks.Count );

			foreach ( IQueuedTaskToken pt in producedTasks )
				Assert.AreEqual( 1, mConsumedTasks.Count( ct => ct.DequeuedTask.Id
					== pt.DequeuedTask.Id ) );
		}

		public void Dispose()
		{
			mConsumedTasks.Clear();
			mConsumedTasks = null;
			mConsumeBufferTask = null;
			mTaskBuffer = null;
		}
	}
}
