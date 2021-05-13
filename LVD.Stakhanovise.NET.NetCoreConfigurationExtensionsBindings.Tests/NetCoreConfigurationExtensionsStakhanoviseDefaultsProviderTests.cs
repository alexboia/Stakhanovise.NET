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
using System.IO;
using System.Text;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Setup;
using NUnit.Framework;
using Moq;
using LVD.Stakhanovise.NET.Model;
using Bogus;

namespace LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings.Tests
{
	[TestFixture]
	public class NetCoreConfigurationExtensionsStakhanoviseDefaultsProviderTests
	{
		[Test]
		public void Test_CanRead_FullConfig ()
		{
			NetCoreConfigurationExtensionsStakhanoviseDefaultsProvider provider =
				new NetCoreConfigurationExtensionsStakhanoviseDefaultsProvider( TestDataDirectory,
					"appsettingssample-full.json",
					"Lvd.Stakhanovise.Net.Config" );

			StakhanoviseSetupDefaults defaults =
				provider.GetDefaults();

			Assert.NotNull( defaults );
			Assert.NotNull( defaults.ExecutorAssemblies );
			Assert.AreEqual( 1, defaults.ExecutorAssemblies.Length );
			Assert.AreEqual( "WinSCPnet.dll", Path.GetFileName( defaults.ExecutorAssemblies[ 0 ].Location ) );

			Assert.NotNull( defaults.CalculateDelayMillisecondsTaskAfterFailure );
			AssertCalculateDelayTicksTaskAfterFailureFnCorrect( defaults,
				expected: ( token ) => ( long )Math.Ceiling( Math.Exp( token.LastQueuedTaskResult.ErrorCount + 1 ) ),
				numberOfRuns: 100 );

			Assert.NotNull( defaults.IsTaskErrorRecoverable );
			AssertIsTaskErrorRecoverableFnCorrect( defaults,
				expected: ( task, exc ) => !( exc is NullReferenceException )
					&& !( exc is ArgumentException )
					&& !( exc is ApplicationException ),
				numberOfRuns: 100 );
		}

		private void AssertCalculateDelayTicksTaskAfterFailureFnCorrect ( StakhanoviseSetupDefaults defaults,
			Func<IQueuedTaskToken, long> expected,
			int numberOfRuns )
		{
			for ( int i = 0; i < numberOfRuns; i++ )
			{
				Mock<IQueuedTaskResult> resultMock = new Mock<IQueuedTaskResult>();
				resultMock.SetupGet( r => r.ErrorCount )
					.Returns( i );

				Mock<IQueuedTaskToken> tokenMock = new Mock<IQueuedTaskToken>();
				tokenMock.SetupGet( t => t.LastQueuedTaskResult )
					.Returns( resultMock.Object );

				long expectedVal = expected
					.Invoke( tokenMock.Object );
				long actualVal = defaults.CalculateDelayMillisecondsTaskAfterFailure
					.Invoke( tokenMock.Object );

				Assert.AreEqual( expectedVal, actualVal );
			}
		}

		private void AssertIsTaskErrorRecoverableFnCorrect ( StakhanoviseSetupDefaults defaults,
			Func<IQueuedTask, Exception, bool> expected,
			int numberOfRuns )
		{
			Faker faker = new Faker();
			Mock<IQueuedTask> taskMock = new Mock<IQueuedTask>();

			//1. Check with exceptions known to return true
			for ( int i = 0; i < numberOfRuns; i++ )
			{
				Assert.IsFalse( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new NullReferenceException( faker.Lorem.Sentence() ) ) );
				Assert.IsFalse( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new ArgumentException( faker.Lorem.Sentence() ) ) );
				Assert.IsFalse( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new ApplicationException( faker.Lorem.Sentence() ) ) );
			}

			//2. Check with exceptions known to return false
			for ( int i = 0; i < numberOfRuns; i++ )
			{
				Assert.IsTrue( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new FileNotFoundException( faker.Lorem.Sentence() ) ) );
				Assert.IsTrue( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new FileLoadException( faker.Lorem.Sentence() ) ) );
				Assert.IsTrue( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new ArithmeticException( faker.Lorem.Sentence() ) ) );
			}

			//3. Check with randomly generated exceptions
			for ( int i = 0; i < numberOfRuns; i++ )
			{
				Exception exc = faker.System.Exception();
				Assert.AreEqual( expected.Invoke( taskMock.Object, exc ), 
					defaults.IsTaskErrorRecoverable.Invoke( taskMock.Object, exc ) );
			}
		}

		private string TestDataDirectory => Path.Combine( Directory.GetCurrentDirectory(),
			"TestData" );
	}
}
