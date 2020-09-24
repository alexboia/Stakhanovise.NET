using LVD.Stakhanovise.NET.Queue;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardTaskQueueTimingBeltSetup : ITaskQueueTimingBeltSetup
	{
		private Func<ITaskQueueTimingBelt> mTimingBeltFactory;

		private StandardPostgreSqlTaskQueueTimingBeltSetup mBuiltInTimingBeltSetup =
			new StandardPostgreSqlTaskQueueTimingBeltSetup();

		public ITaskQueueTimingBeltSetup SetupBuiltInTimingBelt ( Action<IPostgreSqlTaskQueueTimingBeltSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			if ( mTimingBeltFactory != null )
				throw new InvalidOperationException( "Setting up the built-in timing belt is not supported when a custom timing belt has been provided" );

			setupAction.Invoke( mBuiltInTimingBeltSetup );
			return this;
		}

		public ITaskQueueTimingBeltSetup UseTimingBelt ( ITaskQueueTimingBelt timingBelt )
		{
			if ( timingBelt == null )
				throw new ArgumentNullException( nameof( timingBelt ) );

			return UseTimingBeltFactory( () => timingBelt );
		}

		public ITaskQueueTimingBeltSetup UseTimingBeltFactory ( Func<ITaskQueueTimingBelt> timingBeltFactory )
		{
			if ( timingBeltFactory == null )
				throw new ArgumentNullException( nameof( timingBeltFactory ) );

			return this;
		}

		public ITaskQueueTimingBelt BuildTimingBelt ()
		{
			if ( mTimingBeltFactory == null )
				return new PostgreSqlTaskQueueTimingBelt( mBuiltInTimingBeltSetup
					.BuildOptions() );
			else
				return mTimingBeltFactory.Invoke();
		}
	}
}
