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
using LVD.Stakhanovise.NET.Infrastructure;
using NUnit.Framework;
using SqlKata.Execution;
using SqlKata;
using SqlKata.Compilers;

namespace LVD.Stakhanovise.NET.Tests
{
	[TestFixture]
	public class PostgresCompilerExTests
	{
		[Test]
		public void Test_CanGenerateWithRecursive ()
		{
			Query withRecursive = new Query( "tasks_queue_v" )
				.With( "RECURSIVE tasks_queue_v",
					new Query( "[l0]" )
						.FromRaw( "(SELECT pg_try_advisory_lock(100) AS is_lock_acquired) AS [l0]" )
						.Select( "l0.*" ) )
				.Select( "tasks_queue_v.*" );

			PostgresCompilerEx compiler = new PostgresCompilerEx();
			SqlResult withRecursiveResult = compiler.Compile( withRecursive );

			Assert.NotNull( withRecursiveResult );
			Assert.NotNull( withRecursiveResult.Sql );

			Assert.IsTrue( withRecursiveResult.Sql.StartsWith( "WITH RECURSIVE",
				StringComparison.InvariantCultureIgnoreCase ) );
		}
	}
}
