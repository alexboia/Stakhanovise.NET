using LVD.Stakhanovise.NET.Processor;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IAppMetricsMonitorWriterSetup
	{
		IAppMetricsMonitorWriterSetup UseWriter ( IAppMetricsMonitorWriter writer );

		IAppMetricsMonitorWriterSetup UseWriterFactory ( Func<IAppMetricsMonitorWriter> writerFactory );

		IAppMetricsMonitorWriterSetup SetupBuiltInWriter ( Action<IPostgreSqlAppMetricsMonitorWriterSetup> setupAction );
	}
}
