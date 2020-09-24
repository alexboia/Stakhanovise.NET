using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IPostgreSqlExecutionPerformanceMonitorWriterSetup
	{
		IPostgreSqlExecutionPerformanceMonitorWriterSetup WithConnectionOptions ( Action<IConnectionSetup> setupAction );
	}
}
