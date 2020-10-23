using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public class AppMetricsCollection : IAppMetricsProvider
	{
		private Dictionary<AppMetricId, AppMetric> mMetrics;

		private AppMetricsCollection ( Dictionary<AppMetricId, AppMetric> metrics )
		{
			mMetrics = metrics;
		}

		public AppMetricsCollection ( params AppMetricId[] withMetricIds )
		{
			if ( withMetricIds == null || withMetricIds.Length == 0 )
				throw new ArgumentNullException( nameof( withMetricIds ) );

			mMetrics = new Dictionary<AppMetricId, AppMetric>();
			foreach ( AppMetricId metricId in withMetricIds )
				mMetrics.Add( metricId, new AppMetric( metricId, value: 0 ) );
		}

		public AppMetricsCollection ( params AppMetric[] withMetrics )
		{
			if ( withMetrics == null || withMetrics.Length == 0 )
				throw new ArgumentNullException( nameof( withMetrics ) );

			mMetrics = new Dictionary<AppMetricId, AppMetric>();
			foreach ( AppMetric metric in withMetrics )
				mMetrics.Add( metric.Id, metric );
		}

		private static Dictionary<AppMetricId, AppMetric> JoinMetricsFromProviders ( IEnumerable<IAppMetricsProvider> collections )
		{
			Dictionary<AppMetricId, AppMetric> metrics =
				new Dictionary<AppMetricId, AppMetric>();

			foreach ( AppMetricsCollection c in collections )
			{
				foreach ( AppMetric cMetric in c.CollectMetrics() )
				{
					AppMetric cMetricToAdd = cMetric.Copy();
					if ( metrics.TryGetValue( cMetricToAdd.Id, out AppMetric existing ) )
						metrics[ cMetricToAdd.Id ] = existing.JoinWith( cMetricToAdd );
					else
						metrics[ cMetricToAdd.Id ] = cMetricToAdd;
				}
			}

			return metrics;
		}

		public static IEnumerable<AppMetricId> JoinExportedMetrics ( params IAppMetricsProvider[] collections )
		{
			if ( collections == null || collections.Length == 0 )
				throw new ArgumentNullException( nameof( collections ) );

			List<AppMetricId> metricIds =
				new List<AppMetricId>();

			foreach ( AppMetricsCollection c in collections )
			{
				foreach ( AppMetricId cMetricId in c.ExportedMetrics )
					if ( !metricIds.Contains( cMetricId ) )
						metricIds.Add( cMetricId );
			}

			return metricIds;
		}

		public static IEnumerable<AppMetric> JoinCollectMetrics ( params IAppMetricsProvider[] collections )
		{
			if ( collections == null || collections.Length == 0 )
				throw new ArgumentNullException( nameof( collections ) );

			Dictionary<AppMetricId, AppMetric> joinedProviderMetrics =
				JoinMetricsFromProviders( collections );

			return joinedProviderMetrics.Values
				.ToList();
		}

		public static AppMetric JoinQueryMetric ( AppMetricId metricId, params IAppMetricsProvider[] collections )
		{
			if ( metricId == null )
				throw new ArgumentNullException( nameof( metricId ) );

			if ( collections == null || collections.Length == 0 )
				throw new ArgumentNullException( nameof( collections ) );

			AppMetric retVal = null;

			foreach ( AppMetricsCollection c in collections )
			{
				AppMetric cMetric = c.QueryMetric( metricId );
				if ( cMetric != null )
				{
					if ( retVal != null )
						retVal = retVal.JoinWith( cMetric );
					else
						retVal = cMetric.Copy();
				}
			}

			return retVal;
		}

		public static AppMetricsCollection JoinProviders ( params IAppMetricsProvider[] providers )
		{
			if ( providers == null || providers.Length == 0 )
				throw new ArgumentNullException( nameof( providers ) );

			Dictionary<AppMetricId, AppMetric> joinedProviderMetrics =
				JoinMetricsFromProviders( providers );

			return new AppMetricsCollection( joinedProviderMetrics );
		}

		public static AppMetricsCollection JoinProviders ( IEnumerable<IAppMetricsProvider> providers )
		{
			if ( providers == null || providers.Count() == 0 )
				throw new ArgumentNullException( nameof( providers ) );

			Dictionary<AppMetricId, AppMetric> joinedProviderMetrics =
				JoinMetricsFromProviders( providers );

			return new AppMetricsCollection( joinedProviderMetrics );
		}

		public long UpdateMetric ( AppMetricId metricId, Func<AppMetric, long> updateFn )
		{
			if ( metricId == null )
				throw new ArgumentNullException( nameof( metricId ) );

			if ( updateFn == null )
				throw new ArgumentNullException( nameof( updateFn ) );

			if ( mMetrics.TryGetValue( metricId, out AppMetric targetMetric ) )
				return updateFn.Invoke( targetMetric );
			else
				throw new InvalidOperationException( $"Attempted to update unsupported metric: {metricId}" );
		}

		public AppMetric QueryMetric ( AppMetricId metricId )
		{
			if ( mMetrics.TryGetValue( metricId, out AppMetric targetMetric ) )
				return targetMetric.Copy();
			else
				return null;
		}

		public IEnumerable<AppMetric> CollectMetrics ()
		{
			foreach ( AppMetric m in mMetrics.Values )
				yield return m.Copy();
		}

		public IEnumerable<AppMetricId> ExportedMetrics
			=> mMetrics.Keys;
	}
}
