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
using LVD.Stakhanovise.NET.Model;
using System;
using System.Reflection;

namespace LVD.Stakhanovise.NET.Setup
{
	public class ReasonableStakhanoviseDefaultsProvider : IStakhanoviseSetupDefaultsProvider
	{
		public const int DefaultFaultErrorThresholdCount = 5;

		public const int DefaultAppMetricsCollectionIntervalMilliseconds = 10000;


		public StakhanoviseSetupDefaults GetDefaults ()
		{
			int defaultWorkerCount = Math.Max( 1, Environment.ProcessorCount - 1 );

			StakhanoviseSetupDefaults defaults = new StakhanoviseSetupDefaults()
			{
				Mapping = new QueuedTaskMapping(),
				ExecutorAssemblies = GetDefaultAssembliesToScan(),
				WorkerCount = defaultWorkerCount,

				CalculateDelayMillisecondsTaskAfterFailure = token
					=> ( long )Math.Pow( 10, token.LastQueuedTaskResult.ErrorCount + 1 ),

				IsTaskErrorRecoverable = ( task, exc )
					=> !( exc is NullReferenceException )
						&& !( exc is ArgumentException ),

				FaultErrorThresholdCount = DefaultFaultErrorThresholdCount,
				AppMetricsCollectionIntervalMilliseconds = DefaultAppMetricsCollectionIntervalMilliseconds,
				AppMetricsMonitoringEnabled = true,
				SetupBuiltInDbAsssets = true
			};

			return defaults;
		}

		private Assembly[] GetDefaultAssembliesToScan ()
		{
			return new Assembly[]
			{
				Assembly.GetEntryAssembly()
			};
		}
	}
}
