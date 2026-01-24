using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LVD.Stakhanovise.NET.Queue
{
	public class FanOutTaskQueueProducerBuilder
	{
		private class PrioTypeTarget
		{
			public int Priority
			{
				get; set;
			}
			public List<ITaskQueueProducer> Producers { get; } = new List<ITaskQueueProducer>();
		}

		private readonly ITimestampProvider mTimestampProvider;

		private readonly FanOutTaskQueueProducerOptions mOptions;

		private readonly List<FanOutTarget> mGenericTargets;

		private readonly Dictionary<string, PrioTypeTarget> mTypeTargets;

		private FanOutTarget mFallbackTarget;

		public FanOutTaskQueueProducerBuilder( ITimestampProvider timestampProvider,
			FanOutTaskQueueProducerOptions options = null )
		{
			mTimestampProvider = timestampProvider
				?? throw new ArgumentNullException( nameof( timestampProvider ) );
			mOptions = options;
			mGenericTargets = new List<FanOutTarget>();
			mTypeTargets = new Dictionary<string, PrioTypeTarget>();
		}

		public FanOutTaskQueueProducerBuilder AddTarget( IEnumerable<ITaskQueueProducer> producers,
			Func<QueuedTaskProduceInfo, bool> predicate,
			int priority )
		{
			// Validation happens inside FanOutTarget constructor
			mGenericTargets.Add( new FanOutTarget( producers, predicate, priority ) );
			return this;
		}

		public FanOutTaskQueueProducerBuilder WithFallbackTarget( IEnumerable<ITaskQueueProducer> producers )
		{
			// Register a fallback target with the lowest possible priority
			// so it runs only if no strategy with higher priority stopped execution (if stop on match is enabled)
			// or simply as the last evaluated set.
			mFallbackTarget = new FanOutTarget( producers, _ => true, int.MinValue );
			return this;
		}

		public FanOutTaskQueueProducerBuilder WithFallbackTarget( ITaskQueueProducer producer )
		{
			return WithFallbackTarget( new []
			{
				producer
			} );
		}

		public FanOutTaskQueueProducerBuilder WithProducerForPayload<TPayload>( ITaskQueueProducer producer,
			int priority )
		{
			return WithProducerForPayload<TPayload>(
				new []
				{
					producer
				},
				priority
			);
		}

		public FanOutTaskQueueProducerBuilder WithProducerForPayload<TPayload>( IEnumerable<ITaskQueueProducer> producers,
			 int priority )
		{
			if (producers == null)
				throw new ArgumentNullException( nameof( producers ) );

			string typeName = typeof( TPayload ).FullName;

			if (!mTypeTargets.TryGetValue( typeName, out PrioTypeTarget targetInfo ))
			{
				targetInfo = new PrioTypeTarget()
				{
					Priority = priority
				};
				mTypeTargets [ typeName ] = targetInfo;
			}

			// If registered multiple times for same type, merge the targets.
			// We take the max priority to ensure the consolidated rule is respected at the highestrequested level.
			targetInfo.Priority = Math.Max( targetInfo.Priority, priority );
			targetInfo.Producers.AddRange( producers );

			return this;
		}

		public FanOutTaskQueueProducer Build()
		{
			List<FanOutTarget> allTargets = new List<FanOutTarget>( mGenericTargets );

			foreach (KeyValuePair<string, PrioTypeTarget> kvp in mTypeTargets)
			{
				string typeName = kvp.Key;
				PrioTypeTarget info = kvp.Value;

				if (info.Producers.Count > 0)
				{
					allTargets.Add( new FanOutTarget(
						info.Producers,
						( t ) => string.Equals( t.Type, typeName, StringComparison.Ordinal ),
						info.Priority
					) );
				}
			}

			if (mFallbackTarget != null)
				allTargets.Add( mFallbackTarget );

			return new FanOutTaskQueueProducer( allTargets, 
				mTimestampProvider, 
				mOptions );
		}
	}
}