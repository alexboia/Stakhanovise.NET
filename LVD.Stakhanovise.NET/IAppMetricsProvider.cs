using LVD.Stakhanovise.NET.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET
{
	public interface IAppMetricsProvider
	{
		AppMetric QueryMetric ( AppMetricId metricId );

		IEnumerable<AppMetric> CollectMetrics ();

		IEnumerable<AppMetricId> ExportedMetrics { get; }
	}
}
