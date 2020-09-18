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
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class TaskQueueOptions
	{
		public TaskQueueOptions ()
		{
			AbstractTimeTickTimeout = 1000;
			
			Mapping = new QueuedTaskMapping();

			DequeueWithStatuses = new QueuedTaskStatus[] {
				QueuedTaskStatus.Unprocessed,
				QueuedTaskStatus.Error,
				QueuedTaskStatus.Faulted,
				QueuedTaskStatus.Processing
			};

			ConnectionRetryDelay = 100;
			ConnectionRetryCount = 5;
		}

		public int AbstractTimeTickTimeout { get; private set; }

		public int WorkerCount { get; private set; }

		public int ConnectionKeepAlive { get; private set; }

		public string ConnectionString { get; private set; }

		public int FaultErrorThresholdCount { get; private set; }

		public Func<int, long> CalculateDelayTaskAfterFailure { get; private set; }

		public IEnumerable<QueuedTaskStatus> DequeueWithStatuses { get; private set; }

		public int ConnectionRetryCount { get; private set; }

		public int ConnectionRetryDelay { get; private set; }

		public QueuedTaskMapping Mapping { get; private set; }

		public int DequeuePoolSize { get; private set; }
	}
}
