using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NUnit.Framework;
using LVD.Stakhanovise.NET.Setup;
using LVD.Stakhanovise.NET.Model;
using Moq;
using LVD.Stakhanovise.NET.Queue;

namespace LVD.Stakhanovise.NET.Tests.SetupTests
{
	[TestFixture]
	public class ReasonableStakhanoviseDefaultsProviderTests
	{
		private const int TestCalculateDelayMillisecondsErrorCountMax = 100;

		[Test]
		public void Test_CanGetDefaults ()
		{
			ReasonableStakhanoviseDefaultsProvider defaultsProvider =
				new ReasonableStakhanoviseDefaultsProvider();

			StakhanoviseSetupDefaults setupDefaults = defaultsProvider
				.GetDefaults();

			Assert.IsNotNull( setupDefaults );

			Assert.AreEqual( ExpectedDefaultWorkerCount,
				setupDefaults.WorkerCount );

			AssertCorrectDefaultQueuedTaskMapping( setupDefaults );
			AssertCorrectDefaultExecutingAssemblyList( setupDefaults );
			AssertCorrectDefaultCalculateDelayMillisecondsTaskAfterFailureFn( setupDefaults );
			AssertCorrectDefaultIsTaskErrorRecoverableFn( setupDefaults );

			Assert.AreEqual( ReasonableStakhanoviseDefaultsProvider.DefaultFaultErrorThresholdCount,
				setupDefaults.FaultErrorThresholdCount );
			Assert.AreEqual( ReasonableStakhanoviseDefaultsProvider.DefaultAppMetricsCollectionIntervalMilliseconds,
				setupDefaults.AppMetricsCollectionIntervalMilliseconds );

			Assert.IsTrue( setupDefaults.AppMetricsMonitoringEnabled );
			Assert.IsTrue( setupDefaults.SetupBuiltInDbAsssets );
		}

		private void AssertCorrectDefaultQueuedTaskMapping ( StakhanoviseSetupDefaults setupDefaults )
		{
			Assert.NotNull( setupDefaults.Mapping );
			AssertQueuedTaskMappingsMatch( ExpectedDefaultMapping,
				setupDefaults.Mapping );
		}

		private void AssertQueuedTaskMappingsMatch ( QueuedTaskMapping expected, QueuedTaskMapping actual )
		{
			Assert.AreEqual( expected.DequeueFunctionName,
				actual.DequeueFunctionName );
			Assert.AreEqual( expected.ExecutionTimeStatsTableName,
				actual.ExecutionTimeStatsTableName );
			Assert.AreEqual( expected.MetricsTableName,
				actual.MetricsTableName );
			Assert.AreEqual( expected.NewTaskNotificationChannelName,
				actual.NewTaskNotificationChannelName );
			Assert.AreEqual( expected.QueueTableName,
				actual.QueueTableName );
			Assert.AreEqual( expected.ResultsQueueTableName,
				actual.ResultsQueueTableName );
		}

		private void AssertCorrectDefaultExecutingAssemblyList ( StakhanoviseSetupDefaults setupDefaults )
		{
			Assert.NotNull( setupDefaults.ExecutorAssemblies );
			Assert.AreEqual( 1, setupDefaults.ExecutorAssemblies.Length );
			Assert.AreEqual( ExpectedExecutingAssembly, setupDefaults.ExecutorAssemblies[ 0 ] );
		}

		private void AssertCorrectDefaultCalculateDelayMillisecondsTaskAfterFailureFn ( StakhanoviseSetupDefaults setupDefaults )
		{
			Assert.NotNull( setupDefaults.CalculateDelayMillisecondsTaskAfterFailure );
			for ( int iErrorCount = 0; iErrorCount < TestCalculateDelayMillisecondsErrorCountMax; iErrorCount++ )
			{
				long expectedDelayMilliseconds =
					( long )Math.Pow( 10, iErrorCount + 1 );

				IQueuedTaskToken mockTokenWithErrorCount =
					MockQueuedTaskTokenWithErrorCount( iErrorCount );

				long actualDelayMilliseconds = setupDefaults
					.CalculateDelayMillisecondsTaskAfterFailure( mockTokenWithErrorCount );

				Assert.AreEqual( expectedDelayMilliseconds,
					actualDelayMilliseconds );
			}
		}

		private IQueuedTaskToken MockQueuedTaskTokenWithErrorCount ( int errorCount )
		{
			Mock<IQueuedTaskResult> taskResultMock =
				new Mock<IQueuedTaskResult>();
			taskResultMock.SetupGet( r => r.ErrorCount )
				.Returns( errorCount );

			Mock<IQueuedTaskToken> token =
				new Mock<IQueuedTaskToken>();
			token.SetupGet( t => t.LastQueuedTaskResult )
				.Returns( taskResultMock.Object );

			return token.Object;
		}

		private void AssertCorrectDefaultIsTaskErrorRecoverableFn ( StakhanoviseSetupDefaults setupDefaults )
		{
			IQueuedTask mockTask = MockQueuedTask();

			Assert.NotNull( setupDefaults.IsTaskErrorRecoverable );
			Assert.IsFalse( setupDefaults.IsTaskErrorRecoverable( mockTask, new NullReferenceException() ) );
			Assert.IsFalse( setupDefaults.IsTaskErrorRecoverable( mockTask, new ArgumentException() ) );

			Assert.IsTrue( setupDefaults.IsTaskErrorRecoverable( mockTask, new ApplicationException() ) );
			Assert.IsTrue( setupDefaults.IsTaskErrorRecoverable( mockTask, new Exception() ) );
			Assert.IsTrue( setupDefaults.IsTaskErrorRecoverable( mockTask, new NotSupportedException() ) );
		}

		private IQueuedTask MockQueuedTask ()
		{
			return new Mock<IQueuedTask>().Object;
		}

		private int ExpectedDefaultWorkerCount
			=> Math.Max( 1, Environment.ProcessorCount - 1 );

		private QueuedTaskMapping ExpectedDefaultMapping
			=> new QueuedTaskMapping();

		private Assembly ExpectedExecutingAssembly
			=> Assembly.GetEntryAssembly();
	}
}
