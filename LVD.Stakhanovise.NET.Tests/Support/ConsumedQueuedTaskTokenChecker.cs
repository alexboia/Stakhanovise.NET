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
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Tests.Asserts;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests.Support
{
	public class ConsumedQueuedTaskTokenChecker : IDisposable
	{
		private IQueuedTaskToken mPreviousTaskToken = null;

		private TaskQueueDataSource mDataSource = null;

		private List<IQueuedTaskToken> mDequeuedTokens =
			new List<IQueuedTaskToken>();

		public ConsumedQueuedTaskTokenChecker( TaskQueueDataSource dataSource)
		{
			mDataSource = dataSource;
		}

		public void AssertConsumedTokenValid ( IQueuedTaskToken newTaskToken, DateTimeOffset now )
		{
			ClassicAssert.NotNull( newTaskToken );
			ClassicAssert.NotNull( newTaskToken.DequeuedAt );
			ClassicAssert.NotNull( newTaskToken.DequeuedTask );
			ClassicAssert.NotNull( newTaskToken.LastQueuedTaskResult );

			ClassicAssert.IsFalse( mDequeuedTokens.Any( t => t.DequeuedTask.Id == newTaskToken.DequeuedTask.Id ) );

			if ( mPreviousTaskToken != null )
				ClassicAssert.GreaterOrEqual( newTaskToken.DequeuedTask.PostedAtTs, 
					mPreviousTaskToken.DequeuedTask.PostedAtTs );

			mPreviousTaskToken = newTaskToken;
			mDequeuedTokens.Add( newTaskToken );
		}

		public async Task AssertTaskNotInDbAnymoreAsync ( IQueuedTaskToken newTaskToken )
		{
			ClassicAssert.IsNull( await mDataSource.GetQueuedTaskFromDbByIdAsync( newTaskToken
				.DequeuedTask
				.Id ) );
		}

		public async Task AssertTaskResultInDbAndCorrectAsync ( IQueuedTaskToken newTaskToken )
		{
			QueuedTaskResult dbResult = await mDataSource.GetQueuedTaskResultFromDbByIdAsync( newTaskToken
				.DequeuedTask
				.Id );

			AssertQueuedTaskResultMatchesExpectedResult
				.For( newTaskToken.LastQueuedTaskResult )
				.Check( dbResult );
		}

		public void Dispose ()
		{
			mPreviousTaskToken = null;
			mDequeuedTokens.Clear();
		}
	}
}
