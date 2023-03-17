using LVD.Stakhanovise.NET.Model;
using NUnit.Framework;
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

			Assert.IsNotNull( mapping );
			Assert.IsTrue( mapping.IsValid );
			Assert.AreEqual( "sk_tasks_queue_t", mapping.QueueTableName );
			Assert.AreEqual( "sk_task_results_t", mapping.ResultsQueueTableName );
			Assert.AreEqual( "sk_task_execution_time_stats_t", mapping.ExecutionTimeStatsTableName );
			Assert.AreEqual( "sk_metrics_t", mapping.MetricsTableName );
			Assert.AreEqual( "sk_task_queue_item_added", mapping.NewTaskNotificationChannelName );
			Assert.AreEqual( "sk_try_dequeue_task", mapping.DequeueFunctionName );
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

			Assert.IsFalse( mapping.IsValid );
		}

		[Test]
		[TestCase( "" )]
		[TestCase( null )]
		public void Test_IsValid_ReturnsFalse_WhenAnyTableNamesAreNotSet( string emptyValue )
		{
			QueuedTaskMapping mapping = new QueuedTaskMapping();
			mapping.ExecutionTimeStatsTableName = emptyValue;
			Assert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.ResultsQueueTableName = emptyValue;
			Assert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.DequeueFunctionName = emptyValue;
			Assert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.QueueTableName = emptyValue;
			Assert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.NewTaskNotificationChannelName = emptyValue;
			Assert.IsFalse( mapping.IsValid );

			mapping = new QueuedTaskMapping();
			mapping.MetricsTableName = emptyValue;
			Assert.IsFalse( mapping.IsValid );
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

			Assert.AreEqual( $"{prefix}sk_tasks_queue_t", mapping.QueueTableName );
			Assert.AreEqual( $"{prefix}sk_task_results_t", mapping.ResultsQueueTableName );
			Assert.AreEqual( $"{prefix}sk_task_execution_time_stats_t", mapping.ExecutionTimeStatsTableName );
			Assert.AreEqual( $"{prefix}sk_metrics_t", mapping.MetricsTableName );
			Assert.AreEqual( $"{prefix}sk_task_queue_item_added", mapping.NewTaskNotificationChannelName );
			Assert.AreEqual( $"{prefix}sk_try_dequeue_task", mapping.DequeueFunctionName );
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

			Assert.AreEqual( "new_queue", mapping.QueueTableName );
			Assert.AreEqual( "new_results", mapping.ResultsQueueTableName );
			Assert.AreEqual( $"{prefix}sk_task_execution_time_stats_t", mapping.ExecutionTimeStatsTableName );
			Assert.AreEqual( $"{prefix}sk_metrics_t", mapping.MetricsTableName );
			Assert.AreEqual( $"{prefix}sk_task_queue_item_added", mapping.NewTaskNotificationChannelName );
			Assert.AreEqual( $"{prefix}sk_try_dequeue_task", mapping.DequeueFunctionName );
		}
	}
}
