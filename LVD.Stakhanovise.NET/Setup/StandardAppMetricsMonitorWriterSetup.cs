using LVD.Stakhanovise.NET.Model;
using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public class StandardAppMetricsMonitorWriterSetup : IAppMetricsMonitorWriterSetup
	{
		private Func<IAppMetricsMonitorWriter> mWriterFactory;

		private StandardPostgreSqlAppMetricsMonitorWriterSetup mBuiltInWriterSetup;

		public StandardAppMetricsMonitorWriterSetup ( StandardConnectionSetup builtInWriterConnectionSetup,
			StakhanoviseSetupDefaults defaults )
		{
			if ( defaults == null )
				throw new ArgumentNullException( nameof( defaults ) );

			mBuiltInWriterSetup = new StandardPostgreSqlAppMetricsMonitorWriterSetup( builtInWriterConnectionSetup,
				defaults );
		}

		public IAppMetricsMonitorWriterSetup SetupBuiltInWriter ( Action<IPostgreSqlAppMetricsMonitorWriterSetup> setupAction )
		{
			if ( setupAction == null )
				throw new ArgumentNullException( nameof( setupAction ) );

			if ( mWriterFactory != null )
				throw new InvalidOperationException( "Setting up the built-in writer is not supported when a custom writer has been provided" );

			setupAction.Invoke( mBuiltInWriterSetup );
			return this;
		}

		public IAppMetricsMonitorWriterSetup UseWriter ( IAppMetricsMonitorWriter writer )
		{
			if ( writer == null )
				throw new ArgumentNullException( nameof( writer ) );

			return UseWriterFactory( () => writer );
		}

		public IAppMetricsMonitorWriterSetup UseWriterFactory ( Func<IAppMetricsMonitorWriter> writerFactory )
		{
			if ( writerFactory == null )
				throw new ArgumentNullException( nameof( writerFactory ) );

			mWriterFactory = writerFactory;
			return this;
		}

		public IAppMetricsMonitorWriterSetup WithMappingForBuiltInWriter ( QueuedTaskMapping mapping )
		{
			if ( mapping == null )
				throw new ArgumentNullException( nameof( mapping ) );
			mBuiltInWriterSetup.WithMapping( mapping );
			return this;
		}

		public IAppMetricsMonitorWriter BuildWriter ()
		{
			if ( mWriterFactory == null )
				return new PostgreSqlAppMetricsMonitorWriter( mBuiltInWriterSetup
					.BuildOptions() );
			else
				return mWriterFactory.Invoke();
		}
	}
}
