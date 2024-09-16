using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;
using LVD.Stakhanovise.NET.Setup;
using Moq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Reflection;

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

			ClassicAssert.IsNotNull( setupDefaults );

			ClassicAssert.AreEqual( ExpectedDefaultWorkerCount,
				setupDefaults.WorkerCount );

			AssertCorrectDefaultQueuedTaskMapping( setupDefaults );
			AssertCorrectDefaultExecutingAssemblyList( setupDefaults );
			AssertCorrectDefaultCalculateDelayMillisecondsTaskAfterFailureFn( setupDefaults );
			AssertCorrectDefaultIsTaskErrorRecoverableFn( setupDefaults );

			ClassicAssert.AreEqual( ReasonableStakhanoviseDefaultsProvider.DefaultFaultErrorThresholdCount,
				setupDefaults.FaultErrorThresholdCount );
			ClassicAssert.AreEqual( ReasonableStakhanoviseDefaultsProvider.DefaultAppMetricsCollectionIntervalMilliseconds,
				setupDefaults.AppMetricsCollectionIntervalMilliseconds );

			ClassicAssert.IsTrue( setupDefaults.AppMetricsMonitoringEnabled );
			ClassicAssert.IsTrue( setupDefaults.SetupBuiltInDbAsssets );
		}

		private void AssertCorrectDefaultQueuedTaskMapping ( StakhanoviseSetupDefaults setupDefaults )
		{
			ClassicAssert.NotNull( setupDefaults.Mapping );
			AssertQueuedTaskMappingsMatch( ExpectedDefaultMapping,
				setupDefaults.Mapping );
		}

		private void AssertQueuedTaskMappingsMatch ( QueuedTaskMapping expected, QueuedTaskMapping actual )
		{
			ClassicAssert.AreEqual( expected.DequeueFunctionName,
				actual.DequeueFunctionName );
			ClassicAssert.AreEqual( expected.ExecutionTimeStatsTableName,
				actual.ExecutionTimeStatsTableName );
			ClassicAssert.AreEqual( expected.MetricsTableName,
				actual.MetricsTableName );
			ClassicAssert.AreEqual( expected.NewTaskNotificationChannelName,
				actual.NewTaskNotificationChannelName );
			ClassicAssert.AreEqual( expected.QueueTableName,
				actual.QueueTableName );
			ClassicAssert.AreEqual( expected.ResultsQueueTableName,
				actual.ResultsQueueTableName );
		}

		private void AssertCorrectDefaultExecutingAssemblyList ( StakhanoviseSetupDefaults setupDefaults )
		{
			ClassicAssert.NotNull( setupDefaults.ExecutorAssemblies );
			ClassicAssert.AreEqual( 1, setupDefaults.ExecutorAssemblies.Length );
			ClassicAssert.AreEqual( ExpectedExecutingAssembly, setupDefaults.ExecutorAssemblies[ 0 ] );
		}

		private void AssertCorrectDefaultCalculateDelayMillisecondsTaskAfterFailureFn ( StakhanoviseSetupDefaults setupDefaults )
		{
			ClassicAssert.NotNull( setupDefaults.CalculateDelayMillisecondsTaskAfterFailure );
			for ( int iErrorCount = 0; iErrorCount < TestCalculateDelayMillisecondsErrorCountMax; iErrorCount++ )
			{
				long expectedDelayMilliseconds =
					( long )Math.Pow( 10, iErrorCount + 1 );

				IQueuedTaskToken mockTokenWithErrorCount =
					MockQueuedTaskTokenWithErrorCount( iErrorCount );

				long actualDelayMilliseconds = setupDefaults
					.CalculateDelayMillisecondsTaskAfterFailure( mockTokenWithErrorCount );

				ClassicAssert.AreEqual( expectedDelayMilliseconds,
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

			ClassicAssert.NotNull( setupDefaults.IsTaskErrorRecoverable );
			ClassicAssert.IsFalse( setupDefaults.IsTaskErrorRecoverable( mockTask, new NullReferenceException() ) );
			ClassicAssert.IsFalse( setupDefaults.IsTaskErrorRecoverable( mockTask, new ArgumentException() ) );

			ClassicAssert.IsTrue( setupDefaults.IsTaskErrorRecoverable( mockTask, new ApplicationException() ) );
			ClassicAssert.IsTrue( setupDefaults.IsTaskErrorRecoverable( mockTask, new Exception() ) );
			ClassicAssert.IsTrue( setupDefaults.IsTaskErrorRecoverable( mockTask, new NotSupportedException() ) );
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
