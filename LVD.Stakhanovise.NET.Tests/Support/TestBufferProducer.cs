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
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using LVD.Stakhanovise.NET.Queue;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class TestBufferProducer
	{
		private ITaskBuffer mTaskBuffer;

		List<IQueuedTaskToken> mProducedTasks = new List<IQueuedTaskToken>();

		private Type[] mPayloadTypes;

		public TestBufferProducer ( ITaskBuffer buffer, Type[] payloadTypes )
		{
			mTaskBuffer = buffer;
			mPayloadTypes = payloadTypes;
		}

		public Task ProduceTasksAsync ( int numberOfTasks )
		{
			ManualResetEvent bufferSpaceAvailableWaitHandle =
				new ManualResetEvent( false );

			Queue<Type> taskPayloadTypes =
				new Queue<Type>( mPayloadTypes );

			return Task.Run( () =>
			{
				Type currentPayloadType;
				IQueuedTaskToken newTaskToken;
				QueuedTask newTask;
				QueuedTaskResult newLastTaskResult;

				EventHandler handleBufferElementRemoved
					= ( s, e ) => bufferSpaceAvailableWaitHandle.Set();

				mTaskBuffer.QueuedTaskRetrieved
					+= handleBufferElementRemoved;

				while ( taskPayloadTypes.TryDequeue( out currentPayloadType ) )
				{
					for ( int i = 0; i < numberOfTasks; i++ )
					{
						newTask = new QueuedTask( Guid.NewGuid() )
						{
							Payload = Activator.CreateInstance( currentPayloadType ),
							Type = currentPayloadType.FullName
						};

						newLastTaskResult = new QueuedTaskResult( newTask )
						{
							Status = QueuedTaskStatus.Unprocessed
						};

						newTaskToken = new MockQueuedTaskToken( newTask, 
							newLastTaskResult );

						mProducedTasks.Add( newTaskToken );

						while ( !mTaskBuffer.TryAddNewTask( newTaskToken ) )
						{
							bufferSpaceAvailableWaitHandle.WaitOne();
							bufferSpaceAvailableWaitHandle.Reset();
						}
					}
				}

				mTaskBuffer.CompleteAdding();
				mTaskBuffer.QueuedTaskRetrieved
					-= handleBufferElementRemoved;
			} );
		}

		public void AssertMatchesProcessedTasks ( IEnumerable<IQueuedTaskToken> processedTaskTokens )
		{
			Assert.AreEqual( mProducedTasks.Count,
				processedTaskTokens.Count() );

			foreach ( IQueuedTaskToken produced in mProducedTasks )
				Assert.NotNull( processedTaskTokens.FirstOrDefault(
					t => t.DequeuedTask.Id == produced.DequeuedTask.Id ) );
		}
	}
}
