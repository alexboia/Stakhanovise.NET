using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Queue;

namespace LVD.Stakhanovise.NET.Producer.Tests
{
	[TestFixture]
	public class FanOutQueuedTaskTests
	{
		[Test]
		public void Test_Constructor_ThrowsArgumentNullException_WhenAllTasksIsNull()
		{
			Assert.Throws<ArgumentNullException>( () => new FanOutQueuedTask( null, null ) );
		}

		[Test]
		public void Test_Constructor_AcceptsNullErrors()
		{
			QueuedTask task = CreateQueuedTask();
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };

			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			ClassicAssert.NotNull( fanOutTask.Errors );
			ClassicAssert.IsEmpty( fanOutTask.Errors );
		}

		[Test]
		public void Test_Constructor_AcceptsEmptyTasksList()
		{
			List<IQueuedTask> allTasks = new List<IQueuedTask>();

			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			ClassicAssert.NotNull( fanOutTask );
			ClassicAssert.IsEmpty( fanOutTask.AllTasks );
		}

		[Test]
		public void Test_Id_ReturnsGuidEmpty_WhenNoTasks()
		{
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask>(), null );

			Guid id = fanOutTask.Id;

			ClassicAssert.AreEqual( Guid.Empty, id );
		}

		[Test]
		public void Test_Id_ReturnsPrimaryTaskId_WhenTasksExist()
		{
			Guid taskId = Guid.NewGuid();
			QueuedTask task = CreateQueuedTask( taskId );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			Guid id = fanOutTask.Id;

			ClassicAssert.AreEqual( taskId, id );
		}

		[Test]
		public void Test_LockHandleId_ReturnsZero_WhenNoTasks()
		{
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask>(), null );

			long lockHandleId = fanOutTask.LockHandleId;

			ClassicAssert.AreEqual( 0, lockHandleId );
		}

		[Test]
		public void Test_LockHandleId_ReturnsPrimaryTaskLockHandleId_WhenTasksExist()
		{
			QueuedTask task = CreateQueuedTask( lockHandleId: 123 );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			long lockHandleId = fanOutTask.LockHandleId;

			ClassicAssert.AreEqual( 123, lockHandleId );
		}

		[Test]
		public void Test_Type_ReturnsPrimaryTaskType_WhenTasksExist()
		{
			QueuedTask task = CreateQueuedTask( type: "TestTaskType" );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			string type = fanOutTask.Type;

			ClassicAssert.AreEqual( "TestTaskType", type );
		}

		[Test]
		public void Test_Type_ReturnsNull_WhenNoTasks()
		{
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask>(), null );

			string type = fanOutTask.Type;

			ClassicAssert.IsNull( type );
		}

		[Test]
		public void Test_Type_Set_ThrowsNotSupportedException()
		{
			QueuedTask task = CreateQueuedTask();
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			Assert.Throws<NotSupportedException>( () => fanOutTask.Type = "NewType" );
		}

		[Test]
		public void Test_Source_ReturnsPrimaryTaskSource_WhenTasksExist()
		{
			QueuedTask task = CreateQueuedTask( source: "TestSource" );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			string source = fanOutTask.Source;

			ClassicAssert.AreEqual( "TestSource", source );
		}

		[Test]
		public void Test_Source_ReturnsNull_WhenNoTasks()
		{
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask>(), null );

			string source = fanOutTask.Source;

			ClassicAssert.IsNull( source );
		}

		[Test]
		public void Test_Payload_ReturnsPrimaryTaskPayload_WhenTasksExist()
		{
			object payload = new
			{
				Data = "TestData"
			};
			QueuedTask task = CreateQueuedTask( payload: payload );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			object actualPayload = fanOutTask.Payload;

			ClassicAssert.AreSame( payload, actualPayload );
		}

		[Test]
		public void Test_Payload_ReturnsNull_WhenNoTasks()
		{
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask>(), null );

			object payload = fanOutTask.Payload;

			ClassicAssert.IsNull( payload );
		}

		[Test]
		public void Test_Priority_ReturnsPrimaryTaskPriority_WhenTasksExist()
		{
			QueuedTask task = CreateQueuedTask( priority: 10 );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			int priority = fanOutTask.Priority;

			ClassicAssert.AreEqual( 10, priority );
		}

		[Test]
		public void Test_Priority_ReturnsZero_WhenNoTasks()
		{
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask>(), null );

			int priority = fanOutTask.Priority;

			ClassicAssert.AreEqual( 0, priority );
		}

		[Test]
		public void Test_Priority_Set_ThrowsNotSupportedException()
		{
			QueuedTask task = CreateQueuedTask();
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			Assert.Throws<NotSupportedException>( () => fanOutTask.Priority = 5 );
		}

		[Test]
		public void Test_PostedAtTs_ReturnsPrimaryTaskPostedAtTs_WhenTasksExist()
		{
			DateTimeOffset postedAt = DateTimeOffset.Now;
			QueuedTask task = CreateQueuedTask( postedAtTs: postedAt );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			DateTimeOffset actualPostedAt = fanOutTask.PostedAtTs;

			ClassicAssert.AreEqual( postedAt, actualPostedAt );
		}

		[Test]
		public void Test_PostedAtTs_ReturnsMinValue_WhenNoTasks()
		{
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask>(), null );

			DateTimeOffset postedAt = fanOutTask.PostedAtTs;

			ClassicAssert.AreEqual( DateTimeOffset.MinValue, postedAt );
		}

		[Test]
		public void Test_LockedUntilTs_ReturnsPrimaryTaskLockedUntilTs_WhenTasksExist()
		{
			DateTimeOffset lockedUntil = DateTimeOffset.Now.AddMinutes( 5 );
			QueuedTask task = CreateQueuedTask( lockedUntilTs: lockedUntil );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			DateTimeOffset actualLockedUntil = fanOutTask.LockedUntilTs;

			ClassicAssert.AreEqual( lockedUntil, actualLockedUntil );
		}

		[Test]
		public void Test_LockedUntilTs_ReturnsMinValue_WhenNoTasks()
		{
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask>(), null );

			DateTimeOffset lockedUntil = fanOutTask.LockedUntilTs;

			ClassicAssert.AreEqual( DateTimeOffset.MinValue, lockedUntil );
		}

		[Test]
		public void Test_AllTasks_ReturnsAllProvidedTasks()
		{
			QueuedTask task1 = CreateQueuedTask( Guid.NewGuid() );
			QueuedTask task2 = CreateQueuedTask( Guid.NewGuid() );
			QueuedTask task3 = CreateQueuedTask( Guid.NewGuid() );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task1, task2, task3 };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			List<IQueuedTask> actualTasks = fanOutTask.AllTasks.ToList();

			ClassicAssert.AreEqual( 3, actualTasks.Count );
			CollectionAssert.Contains( actualTasks, task1 );
			CollectionAssert.Contains( actualTasks, task2 );
			CollectionAssert.Contains( actualTasks, task3 );
		}

		[Test]
		public void Test_AllTasks_IsImmutable()
		{
			QueuedTask task = CreateQueuedTask();
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			allTasks.Add( CreateQueuedTask() );

			ClassicAssert.AreEqual( 1, fanOutTask.AllTasks.Count() );
		}

		[Test]
		public void Test_Errors_ReturnsAllProvidedErrors()
		{
			InvalidOperationException error1 = new InvalidOperationException( "Error 1" );
			ArgumentException error2 = new ArgumentException( "Error 2" );
			List<Exception> errors = new List<Exception> { error1, error2 };
			QueuedTask task = CreateQueuedTask();
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask> { task }, errors );

			List<Exception> actualErrors = fanOutTask.Errors.ToList();

			ClassicAssert.AreEqual( 2, actualErrors.Count );
			CollectionAssert.Contains( actualErrors, error1 );
			CollectionAssert.Contains( actualErrors, error2 );
		}

		[Test]
		public void Test_Errors_IsImmutable()
		{
			List<Exception> errors = new List<Exception> { new Exception( "Error" ) };
			QueuedTask task = CreateQueuedTask();
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( new List<IQueuedTask> { task }, errors );

			errors.Add( new Exception( "Another Error" ) );

			ClassicAssert.AreEqual( 1, fanOutTask.Errors.Count() );
		}

		[Test]
		public void Test_PrimaryTask_IsFirstTask_WhenMultipleTasksProvided()
		{
			QueuedTask task1 = CreateQueuedTask( Guid.NewGuid(), priority: 10 );
			QueuedTask task2 = CreateQueuedTask( Guid.NewGuid(), priority: 20 );
			QueuedTask task3 = CreateQueuedTask( Guid.NewGuid(), priority: 30 );
			List<IQueuedTask> allTasks = new List<IQueuedTask> { task1, task2, task3 };
			FanOutQueuedTask fanOutTask = new FanOutQueuedTask( allTasks, null );

			ClassicAssert.AreEqual( task1.Id, fanOutTask.Id );
			ClassicAssert.AreEqual( task1.Priority, fanOutTask.Priority );
		}

		private QueuedTask CreateQueuedTask(
			Guid? id = null,
			long lockHandleId = 0,
			string type = "TestType",
			string source = "TestSource",
			object payload = null,
			int priority = 0,
			DateTimeOffset? postedAtTs = null,
			DateTimeOffset? lockedUntilTs = null )
		{
			return new QueuedTask
			{
				Id = id ?? Guid.NewGuid(),
				LockHandleId = lockHandleId,
				Type = type,
				Source = source,
				Payload = payload,
				Priority = priority,
				PostedAtTs = postedAtTs ?? DateTimeOffset.Now,
				LockedUntilTs = lockedUntilTs ?? DateTimeOffset.Now
			};
		}
	}
}
