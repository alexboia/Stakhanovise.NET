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
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Setup;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;

namespace LVD.Stakhanovise.NET.NetCoreConfigurationExtensionsBindings.Tests
{
	[TestFixture]
	public class NetCoreConfigurationStakhanoviseDefaultsProviderTests
	{
		private const string SampleSettingsFileFull = "appsettingssample-full.json";

		private const string SampleSettingsFileConnStringOnly = "appsettingssample-connstringonly.json";

		private const string SampleSettingsFileConnStringAndMapping = "appsettingssample-connstring+mapping.json";

		private const string SampleSettingsFileExecutorAssembliesOnly = "appsettingssample-assembliesonly.json";

		private const string SampleSettingsFileEmptySection = "appsettingssample-emptysection.json";

		private const string TestConnectionString = "Host=localmotherland;Port=61117;Database=coal_mining_db;Username=postgres;Password=forthemotherland1917;";

		[Test]
		[Repeat( 5 )]
		public void Test_CanRead_EmptySection ()
		{
			ReasonableStakhanoviseDefaultsProvider reasonableDefaultsProvider =
				new ReasonableStakhanoviseDefaultsProvider();

			NetCoreConfigurationStakhanoviseDefaultsProvider provider =
				new NetCoreConfigurationStakhanoviseDefaultsProvider( TestDataDirectory,
					SampleSettingsFileEmptySection,
					"Lvd.Stakhanovise.Net.Config" );

			StakhanoviseSetupDefaults defaults =
				provider.GetDefaults();

			StakhanoviseSetupDefaults reasonableDefaults =
				reasonableDefaultsProvider.GetDefaults();

			ClassicAssert.NotNull( defaults );

			AssertDefaultsFromConfigMatchReasonableDefaults( defaults,
				reasonableDefaults );

			AssertExecutorAssembliesMatchReasonableDefaultsAssemblies( defaults,
				reasonableDefaults );

			AssertConnectionStringEmpty( defaults );

			AssertMappingMatchesReasonableDefaultsMapping( defaults,
				reasonableDefaults );
		}

		private void AssertConnectionStringEmpty ( StakhanoviseSetupDefaults defaults )
		{
			ClassicAssert.Null( defaults.ConnectionString );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_CanRead_ConnStringOnly ()
		{
			ReasonableStakhanoviseDefaultsProvider reasonableDefaultsProvider =
				new ReasonableStakhanoviseDefaultsProvider();

			NetCoreConfigurationStakhanoviseDefaultsProvider provider =
				new NetCoreConfigurationStakhanoviseDefaultsProvider( TestDataDirectory,
					SampleSettingsFileConnStringOnly,
					"Lvd.Stakhanovise.Net.Config" );

			StakhanoviseSetupDefaults defaults =
				provider.GetDefaults();

			StakhanoviseSetupDefaults reasonableDefaults =
				reasonableDefaultsProvider.GetDefaults();

			ClassicAssert.NotNull( defaults );

			AssertDefaultsFromConfigMatchReasonableDefaults( defaults,
				reasonableDefaults );

			AssertExecutorAssembliesMatchReasonableDefaultsAssemblies( defaults,
				reasonableDefaults );

			AssertConnectionStringCorrect( defaults );

			AssertMappingMatchesReasonableDefaultsMapping( defaults,
				reasonableDefaults );
		}

		private void AssertConnectionStringCorrect ( StakhanoviseSetupDefaults defaults )
		{
			ClassicAssert.NotNull( defaults.ConnectionString );
			ClassicAssert.IsNotEmpty( defaults.ConnectionString );
			ClassicAssert.AreEqual( TestConnectionString, defaults.ConnectionString );
		}

		private void AssertExecutorAssembliesMatchReasonableDefaultsAssemblies ( StakhanoviseSetupDefaults defaults,
			StakhanoviseSetupDefaults reasonableDefaults )
		{
			ClassicAssert.AreEqual( reasonableDefaults.ExecutorAssemblies.Length,
				defaults.ExecutorAssemblies.Length );
			CollectionAssert.AreEqual( reasonableDefaults.ExecutorAssemblies,
				defaults.ExecutorAssemblies );
		}

		private void AssertDefaultsFromConfigMatchReasonableDefaults ( StakhanoviseSetupDefaults defaults,
			StakhanoviseSetupDefaults reasonableDefaults )
		{
			ClassicAssert.AreEqual( reasonableDefaults.WorkerCount,
				defaults.WorkerCount );
			ClassicAssert.AreEqual( reasonableDefaults.CalculateDelayMillisecondsTaskAfterFailure,
				defaults.CalculateDelayMillisecondsTaskAfterFailure );
			ClassicAssert.AreEqual( reasonableDefaults.IsTaskErrorRecoverable,
				defaults.IsTaskErrorRecoverable );
			ClassicAssert.AreEqual( reasonableDefaults.FaultErrorThresholdCount,
				defaults.FaultErrorThresholdCount );
			ClassicAssert.AreEqual( reasonableDefaults.AppMetricsCollectionIntervalMilliseconds,
				defaults.AppMetricsCollectionIntervalMilliseconds );
			ClassicAssert.AreEqual( reasonableDefaults.AppMetricsMonitoringEnabled,
				defaults.AppMetricsMonitoringEnabled );
			ClassicAssert.AreEqual( reasonableDefaults.SetupBuiltInDbAsssets,
				defaults.SetupBuiltInDbAsssets );
		}

		private void AssertMappingMatchesReasonableDefaultsMapping ( StakhanoviseSetupDefaults defaults,
			StakhanoviseSetupDefaults reasonableDefaults )
		{
			QueuedTaskMapping defaultMapping =
				reasonableDefaults.Mapping;
			AssertMappingsEqual( defaultMapping,
				defaults.Mapping );
		}

		private void AssertMappingsEqual ( QueuedTaskMapping expected, QueuedTaskMapping actual )
		{
			ClassicAssert.NotNull( actual );
			ClassicAssert.AreEqual( expected.QueueTableName,
				actual.QueueTableName );
			ClassicAssert.AreEqual( expected.ResultsQueueTableName,
				actual.ResultsQueueTableName );
			ClassicAssert.AreEqual( expected.NewTaskNotificationChannelName,
				actual.NewTaskNotificationChannelName );
			ClassicAssert.AreEqual( expected.ExecutionTimeStatsTableName,
				actual.ExecutionTimeStatsTableName );
			ClassicAssert.AreEqual( expected.MetricsTableName,
				actual.MetricsTableName );
			ClassicAssert.AreEqual( expected.DequeueFunctionName,
				actual.DequeueFunctionName );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_CanRead_FullConfig ()
		{
			NetCoreConfigurationStakhanoviseDefaultsProvider provider =
				new NetCoreConfigurationStakhanoviseDefaultsProvider( TestDataDirectory,
					SampleSettingsFileFull,
					"Lvd.Stakhanovise.Net.Config" );

			StakhanoviseSetupDefaults defaults =
				provider.GetDefaults();

			ClassicAssert.NotNull( defaults );

			ClassicAssert.AreEqual( 12, defaults.WorkerCount );
			ClassicAssert.AreEqual( 13, defaults.FaultErrorThresholdCount );
			ClassicAssert.AreEqual( 1234, defaults.AppMetricsCollectionIntervalMilliseconds );
			ClassicAssert.AreEqual( true, defaults.AppMetricsMonitoringEnabled );
			ClassicAssert.AreEqual( true, defaults.SetupBuiltInDbAsssets );

			AssertExecutorAssembliesMatchTestAssemblies( defaults );

			ClassicAssert.NotNull( defaults.CalculateDelayMillisecondsTaskAfterFailure );
			AssertCalculateDelayTicksTaskAfterFailureFnCorrect( defaults,
				expected: ( token ) => ( long )Math.Ceiling( Math.Exp( token.LastQueuedTaskResult.ErrorCount + 1 ) ),
				numberOfRuns: 100 );

			ClassicAssert.NotNull( defaults.IsTaskErrorRecoverable );
			AssertIsTaskErrorRecoverableFnCorrect( defaults,
				expected: ( task, exc ) => !( exc is NullReferenceException )
					&& !( exc is ArgumentException )
					&& !( exc is ApplicationException ),
				numberOfRuns: 100 );

			AssertConnectionStringCorrect( defaults );
			AssertMappingMatchesNonDefaultTestMapping( defaults );
		}

		private void AssertExecutorAssembliesMatchTestAssemblies ( StakhanoviseSetupDefaults defaults )
		{
			ClassicAssert.NotNull( defaults.ExecutorAssemblies );
			ClassicAssert.AreEqual( 1, defaults.ExecutorAssemblies.Length );
			ClassicAssert.AreEqual( "WinSCPnet.dll", Path.GetFileName( defaults.ExecutorAssemblies[ 0 ].Location ) );
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

				ClassicAssert.AreEqual( expectedVal, actualVal );
			}
		}

		private void AssertIsTaskErrorRecoverableFnCorrect ( StakhanoviseSetupDefaults defaults,
			Func<IQueuedTask, Exception, bool> expected,
			int numberOfRuns )
		{
			AssertIsTaskErrorRecoverableFnCorrectForNonRecoverableErrors( defaults,
				numberOfRuns );

			AssertIsTaskErrorRecoverableFnCorrectForRecoverableErrors( defaults,
				numberOfRuns );

			AssertIsTascKErrorRecoverableFnCorrectCompareWithExpectedFn( defaults,
				expected,
				numberOfRuns );
		}

		private void AssertIsTaskErrorRecoverableFnCorrectForNonRecoverableErrors ( StakhanoviseSetupDefaults defaults, int numberOfRuns )
		{
			Faker faker = new Faker();
			Mock<IQueuedTask> taskMock = new Mock<IQueuedTask>();

			for ( int i = 0; i < numberOfRuns; i++ )
			{
				ClassicAssert.IsFalse( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new NullReferenceException( faker.Lorem.Sentence() ) ) );
				ClassicAssert.IsFalse( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new ArgumentException( faker.Lorem.Sentence() ) ) );
				ClassicAssert.IsFalse( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new ApplicationException( faker.Lorem.Sentence() ) ) );
			}
		}

		private void AssertIsTaskErrorRecoverableFnCorrectForRecoverableErrors ( StakhanoviseSetupDefaults defaults, int numberOfRuns )
		{
			Faker faker = new Faker();
			Mock<IQueuedTask> taskMock = new Mock<IQueuedTask>();

			for ( int i = 0; i < numberOfRuns; i++ )
			{
				ClassicAssert.IsTrue( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new FileNotFoundException( faker.Lorem.Sentence() ) ) );
				ClassicAssert.IsTrue( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new FileLoadException( faker.Lorem.Sentence() ) ) );
				ClassicAssert.IsTrue( defaults.IsTaskErrorRecoverable( taskMock.Object,
					new ArithmeticException( faker.Lorem.Sentence() ) ) );
			}
		}

		private void AssertIsTascKErrorRecoverableFnCorrectCompareWithExpectedFn ( StakhanoviseSetupDefaults defaults,
			Func<IQueuedTask, Exception, bool> expected,
			int numberOfRuns )
		{
			Faker faker = new Faker();
			Mock<IQueuedTask> taskMock = new Mock<IQueuedTask>();

			for ( int i = 0; i < numberOfRuns; i++ )
			{
				Exception exc = faker.System.Exception();
				ClassicAssert.AreEqual( expected.Invoke( taskMock.Object, exc ),
					defaults.IsTaskErrorRecoverable.Invoke( taskMock.Object, exc ) );
			}
		}

		private void AssertMappingMatchesNonDefaultTestMapping ( StakhanoviseSetupDefaults defaults )
		{
			QueuedTaskMapping nonDefaultTestMapping =
				GetNonDefaultTestMapping();
			AssertMappingsEqual( nonDefaultTestMapping,
				defaults.Mapping );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_CanRead_ConnStringAndMapping ()
		{
			ReasonableStakhanoviseDefaultsProvider reasonableDefaultsProvider =
				new ReasonableStakhanoviseDefaultsProvider();

			NetCoreConfigurationStakhanoviseDefaultsProvider provider =
				new NetCoreConfigurationStakhanoviseDefaultsProvider( TestDataDirectory,
					SampleSettingsFileConnStringAndMapping,
					"Lvd.Stakhanovise.Net.Config" );

			StakhanoviseSetupDefaults defaults =
				provider.GetDefaults();

			StakhanoviseSetupDefaults reasonableDefaults =
				reasonableDefaultsProvider.GetDefaults();

			ClassicAssert.NotNull( defaults );

			AssertDefaultsFromConfigMatchReasonableDefaults( defaults,
				reasonableDefaults );

			AssertConnectionStringCorrect( defaults );
			AssertMappingMatchesNonDefaultTestMapping( defaults );
		}

		[Test]
		[Repeat( 5 )]
		public void Test_CanRead_ExecutorAssembliesOnly ()
		{
			ReasonableStakhanoviseDefaultsProvider reasonableDefaultsProvider =
				new ReasonableStakhanoviseDefaultsProvider();

			NetCoreConfigurationStakhanoviseDefaultsProvider provider =
				new NetCoreConfigurationStakhanoviseDefaultsProvider( TestDataDirectory,
					SampleSettingsFileExecutorAssembliesOnly,
					"Lvd.Stakhanovise.Net.Config" );

			StakhanoviseSetupDefaults defaults =
				provider.GetDefaults();

			StakhanoviseSetupDefaults reasonableDefaults =
				reasonableDefaultsProvider.GetDefaults();

			ClassicAssert.NotNull( defaults );

			AssertDefaultsFromConfigMatchReasonableDefaults( defaults,
				reasonableDefaults );

			AssertConnectionStringEmpty( defaults );

			AssertMappingMatchesReasonableDefaultsMapping( defaults,
				reasonableDefaults );

			AssertExecutorAssembliesMatchTestAssemblies( defaults );
		}

		private QueuedTaskMapping GetNonDefaultTestMapping ()
		{
			return new QueuedTaskMapping()
			{
				QueueTableName = "sk1_queue_t",
				ResultsQueueTableName = "sk1_results_queue_t",
				NewTaskNotificationChannelName = "sk1_new_task_posted",
				ExecutionTimeStatsTableName = "sk1_task_execution_time_stats_t",
				MetricsTableName = "sk1_metrics_t",
				DequeueFunctionName = "sk1_try_dequeue_task"
			};
		}

		private string TestDataDirectory => Path.Combine( Directory.GetCurrentDirectory(),
			"TestData" );
	}
}
