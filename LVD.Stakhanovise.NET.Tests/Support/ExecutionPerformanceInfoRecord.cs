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

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class ExecutionPerformanceInfoRecord : IEquatable<ExecutionPerformanceInfoRecord>
	{
		public bool Equals ( ExecutionPerformanceInfoRecord other )
		{
			return other != null &&
				string.Equals( PayloadType, other.PayloadType ) &&
				NExecutionCycles == other.NExecutionCycles &&
				LastExecutionTime == other.LastExecutionTime &&
				AvgExecutionTime == other.AvgExecutionTime &&
				FastestExecutionTime == other.FastestExecutionTime &&
				LongestExecutionTime == other.LongestExecutionTime &&
				TotalExecutionTime == other.TotalExecutionTime;
		}

		public override bool Equals ( object obj )
		{
			return Equals( obj as ExecutionPerformanceInfoRecord );
		}

		public override int GetHashCode ()
		{
			int result = 1;

			result = result * 13 + PayloadType.GetHashCode();
			result = result * 13 + NExecutionCycles.GetHashCode();
			result = result * 13 + LastExecutionTime.GetHashCode();
			result = result * 13 + AvgExecutionTime.GetHashCode();
			result = result * 13 + FastestExecutionTime.GetHashCode();
			result = result * 13 + LongestExecutionTime.GetHashCode();
			result = result * 13 + TotalExecutionTime.GetHashCode();

			return result;
		}

		public bool AllZeroValues ()
		{
			return LastExecutionTime == 0
				&& AvgExecutionTime == 0
				&& FastestExecutionTime == 0
				&& LongestExecutionTime == 0
				&& TotalExecutionTime == 0;
		}

		public string PayloadType { get; set; }

		public long NExecutionCycles { get; set; }

		public long LastExecutionTime { get; set; }

		public long AvgExecutionTime { get; set; }

		public long FastestExecutionTime { get; set; }

		public long LongestExecutionTime { get; set; }

		public long TotalExecutionTime { get; set; }
	}
}
