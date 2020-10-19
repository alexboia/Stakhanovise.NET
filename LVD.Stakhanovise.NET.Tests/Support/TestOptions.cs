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
using Bogus;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public static class TestOptions
	{
		public static readonly QueuedTaskMapping DefaultMapping =
			new QueuedTaskMapping();

		public static readonly int DefaultFaultErrorThresholdCount = 5;

		public static TaskProcessingOptions GetDefaultTaskProcessingOptions ()
		{
			return new TaskProcessingOptions( calculateDelayTicksTaskAfterFailure: token
					=> ( long )500 * ( token.LastQueuedTaskResult.ErrorCount + 1 ),
				isTaskErrorRecoverable: ( task, exc )
					 => !( exc is NullReferenceException )
						 && !( exc is ArgumentException ),
				faultErrorThresholdCount: DefaultFaultErrorThresholdCount );
		}

		public static TaskQueueConsumerOptions GetDefaultTaskQueueConsumerOptions ( string connectionString )
		{
			return new TaskQueueConsumerOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 5,
					retryCount: 3,
					retryDelayMilliseconds: 250 ),
				mapping: DefaultMapping,
				queueConsumerConnectionPoolSize: 10 );
		}

		public static TaskQueueInfoOptions GetDefaultTaskQueueInfoOptions ( string connectionString )
		{
			return new TaskQueueInfoOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 250 ),
				mapping: DefaultMapping );
		}

		public static TaskQueueOptions GetDefaultTaskQueueProducerAndResultOptions ( string connectionString )
		{
			return new TaskQueueOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 250 ),
				DefaultMapping );
		}

		public static TaskQueueOptions GetDefaultTaskResultQueueOptions ( string connectionString )
		{
			return new TaskQueueOptions( new ConnectionOptions( connectionString,
					keepAliveSeconds: 0,
					retryCount: 3,
					retryDelayMilliseconds: 250 ),
				DefaultMapping );
		}

		public static PostgreSqlExecutionPerformanceMonitorWriterOptions GetDefaultPostgreSqlExecutionPerformanceMonitorWriterOptions ( string connectionString )
		{
			return new PostgreSqlExecutionPerformanceMonitorWriterOptions( new ConnectionOptions( connectionString,
				keepAliveSeconds: 0,
				retryCount: 3,
				retryDelayMilliseconds: 250 ) );
		}
	}
}
