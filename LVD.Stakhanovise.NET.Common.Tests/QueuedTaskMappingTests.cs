using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;

namespace LVD.Stakhanovise.NET.Common.Tests
{
	[TestFixture]
	public class QueuedTaskMappingTests
	{
		[Test]
		public void Test_DefaultMappingChecks()
		{
			QueuedTaskMapping mapping = QueuedTaskMapping.Default;

			ClassicAssert.IsNotNull( mapping );
			ClassicAssert.IsTrue( mapping.IsValid );
			ClassicAssert.AreEqual( "sk_tasks_queue_t", mapping.QueueTableName );
			ClassicAssert.AreEqual( "sk_task_results_t", mapping.ResultsQueueTableName );
			ClassicAssert.AreEqual( "sk_task_execution_time_stats_t", mapping.ExecutionTimeStatsTableName );
			ClassicAssert.AreEqual( "sk_metrics_t", mapping.MetricsTableName );
			ClassicAssert.AreEqual( "sk_task_queue_item_added", mapping.NewTaskNotificationChannelName );
			ClassicAssert.AreEqual( "sk_try_dequeue_task", mapping.DequeueFunctionName );
		}

		[Test]
		public void Test_IsValid_ReturnsFalse_WhenAllPropertiesAreEmpty()
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping()
			{
				DequeueFunctionName = null,
				ExecutionTimeStatsTableName = null,
				MetricsTableName = null,
				NewTaskNotificationChannelName = null,
				QueueTableName = null,
				ResultsQueueTableName = null
			};

			ClassicAssert.IsFalse( mapping.IsValid );
		}

		[Test]
		[TestCase( "" )]
		[TestCase( null )]
		public void Test_IsValid_ReturnsFalse_WhenAnyTableNamesAreNotSet( string emptyValue )
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.ExecutionTimeStatsTableName = emptyValue;
			ClassicAssert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.ResultsQueueTableName = emptyValue;
			ClassicAssert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.DequeueFunctionName = emptyValue;
			ClassicAssert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.QueueTableName = emptyValue;
			ClassicAssert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.NewTaskNotificationChannelName = emptyValue;
			ClassicAssert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.MetricsTableName = emptyValue;
			ClassicAssert.IsFalse( mapping.IsValid );
		}

		[Test]
		[TestCase( "test_" )]
		public void Test_DefaultWithPrefix_NullModifier_ThrowsException( string prefix )
		{
			Assert.Throws<ArgumentNullException>( () => QueuedTaskMapping.DefaultWithPrefix( prefix, null ) );
		}

		[Test]
		[TestCase( "test_" )]
		public void Test_DefaultWithPrefix_ReturnsDefaultWithPrefixAdded( string prefix )
		{
			QueuedTaskMapping mapping = QueuedTaskMapping.DefaultWithPrefix( prefix );

			ClassicAssert.AreEqual( $"{prefix}sk_tasks_queue_t", mapping.QueueTableName );
			ClassicAssert.AreEqual( $"{prefix}sk_task_results_t", mapping.ResultsQueueTableName );
			ClassicAssert.AreEqual( $"{prefix}sk_task_execution_time_stats_t", mapping.ExecutionTimeStatsTableName );
			ClassicAssert.AreEqual( $"{prefix}sk_metrics_t", mapping.MetricsTableName );
			ClassicAssert.AreEqual( $"{prefix}sk_task_queue_item_added", mapping.NewTaskNotificationChannelName );
			ClassicAssert.AreEqual( $"{prefix}sk_try_dequeue_task", mapping.DequeueFunctionName );
		}

		[Test]
		[TestCase( "test_" )]
		public void Test_efaultWithPrefix_WithModifier_ModifiesMapping( string prefix )
		{
			QueuedTaskMapping mapping = QueuedTaskMapping.DefaultWithPrefix( prefix, m =>
			{
				m.QueueTableName = "new_queue";
				m.ResultsQueueTableName = "new_results";
			} );

			ClassicAssert.AreEqual( "new_queue", mapping.QueueTableName );
			ClassicAssert.AreEqual( "new_results", mapping.ResultsQueueTableName );
			ClassicAssert.AreEqual( $"{prefix}sk_task_execution_time_stats_t", mapping.ExecutionTimeStatsTableName );
			ClassicAssert.AreEqual( $"{prefix}sk_metrics_t", mapping.MetricsTableName );
			ClassicAssert.AreEqual( $"{prefix}sk_task_queue_item_added", mapping.NewTaskNotificationChannelName );
			ClassicAssert.AreEqual( $"{prefix}sk_try_dequeue_task", mapping.DequeueFunctionName );
		}
	}
}
