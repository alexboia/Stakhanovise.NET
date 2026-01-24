using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Queue
{
	public class FanOutTaskQueueProducer : ITaskQueueProducer
	{
		private class EnqueueResult
		{
			public IQueuedTask Task
			{
				get; set;
			}

			public Exception Error
			{
				get; set;
			}

			public bool IsSuccess => Error == null;
		}

		private readonly IEnumerable<FanOutTarget> mTargets;

		private readonly ITimestampProvider mTimestampProvider;

		private readonly FanOutTaskQueueProducerOptions mOptions;

		public FanOutTaskQueueProducer( IEnumerable<FanOutTarget> targets,
			ITimestampProvider timestampProvider,
			FanOutTaskQueueProducerOptions options = null )
		{
			if (targets == null)
				throw new ArgumentNullException( nameof( targets ) );

			// 1. Materialize targets to avoid multiple enumerations 
			// if the source is a lazy LINQ query.
			mTargets = targets.ToList();
			mTimestampProvider = timestampProvider
				?? throw new ArgumentNullException( nameof( timestampProvider ) );
			mOptions = options
				?? new FanOutTaskQueueProducerOptions();
		}

		public async Task<IQueuedTask> EnqueueAsync<TPayload>( TPayload payload, string source, int priority )
		{
			if (EqualityComparer<TPayload>.Default.Equals( payload, default( TPayload ) ))
				throw new ArgumentNullException( nameof( payload ) );

			if (string.IsNullOrEmpty( source ))
				throw new ArgumentNullException( nameof( source ) );

			if (priority < 0)
				throw new ArgumentOutOfRangeException( nameof( priority ), "Priority must be greater than or equal to 0" );

			return await EnqueueAsync( new QueuedTaskProduceInfo()
			{
				Payload = payload,
				Type = DeterminePayloadTypeFullName<TPayload>(),
				Priority = priority,
				Source = source,
				LockedUntilTs = GenerateImmediatePastLockedTimestamp(),
				Status = QueuedTaskStatus.Unprocessed
			} );
		}

		public async Task<IQueuedTask> EnqueueAsync( QueuedTaskProduceInfo queuedTaskInfo )
		{
			if (queuedTaskInfo == null)
				throw new ArgumentNullException( nameof( queuedTaskInfo ) );

			// 1. Ensure we have a consistent ID across all fan-out targets
			if (!queuedTaskInfo.HasId && mOptions.CorrelateIds)
				queuedTaskInfo.Id = Guid.NewGuid();

			// 2. Identify all unique producers
			List<ITaskQueueProducer> producers =
				CollectMatchingProducers( queuedTaskInfo );

			if (producers.Count == 0)
			{
				if (mOptions.ThrowIfNoTargetsMatched)
					throw new InvalidOperationException( "No fan-out targets matched for the given task." );

				return queuedTaskInfo.CreateNewTask( mTimestampProvider );
			}

			// 3. Execute Fan-Out safely (capturing individual failures)
			List<Task<EnqueueResult>> executionTasks = producers
				.Select( p => AttemptEnqueueAsync( p, queuedTaskInfo ) )
				.ToList();

			EnqueueResult [] results = await Task.WhenAll( executionTasks );

			// 4. Apply Error Handling Policy
			return ProcessResults( results );
		}

		private IQueuedTask ProcessResults( EnqueueResult [] results )
		{
			List<EnqueueResult> successful = results
				.Where( r => r.IsSuccess )
				.ToList();

			List<EnqueueResult> failures = results
				.Where( r => !r.IsSuccess )
				.ToList();

			if (mOptions.ErrorPolicy == FanOutErrorPolicy.ThrowOnAnyError)
			{
				if (failures.Count > 0)
				{
					throw new AggregateException(
						"One or more fan-out producers failed to enqueue the task.",
						failures.Select( f => f.Error )
					);
				}
			}
			else if (mOptions.ErrorPolicy == FanOutErrorPolicy.SucceedIfAny)
			{
				if (successful.Count == 0 && failures.Count > 0)
				{
					// If everything failed, we must throw.
					throw new AggregateException(
						"All fan-out producers failed to enqueue the task.",
						failures.Select( f => f.Error )
					);
				}
			}

			// Return the first successful task representation as the handle
			// (If partial success is allowed, this gives the caller a valid handle)
			return new FanOutQueuedTask(
				successful.Select( r => r.Task ),
				failures.Select( r => r.Error )
			);
		}

		private async Task<EnqueueResult> AttemptEnqueueAsync( ITaskQueueProducer producer, QueuedTaskProduceInfo info )
		{
			try
			{
				IQueuedTask result = await producer.EnqueueAsync( info.Copy() );
				return new EnqueueResult
				{
					Task = result,
					Error = null
				};
			}
			catch (Exception ex)
			{
				return new EnqueueResult
				{
					Task = null,
					Error = ex
				};
			}
		}

		private List<ITaskQueueProducer> CollectMatchingProducers( QueuedTaskProduceInfo info )
		{
			HashSet<ITaskQueueProducer> distinctProducers =
				new HashSet<ITaskQueueProducer>();

			foreach (FanOutTarget target in mTargets.OrderByDescending( t => t.Priority ))
			{
				if (target.Predicate != null && target.Predicate( info ))
				{
					if (target.Producers != null && target.Producers.Any())
					{
						// AddRange equivalent for HashSet
						foreach (ITaskQueueProducer p in target.Producers)
							distinctProducers.Add( p );

						if (mOptions.StopOnFirstMatch)
							break;
					}
				}
			}

			return distinctProducers.ToList();
		}

		private string DeterminePayloadTypeFullName<TPayload>()
		{
			return typeof( TPayload ).FullName;
		}

		private DateTimeOffset GenerateImmediatePastLockedTimestamp()
		{
			return mTimestampProvider.GetNow();
		}
	}
}