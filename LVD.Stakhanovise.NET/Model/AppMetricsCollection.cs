// 
// BSD 3-Clause License
// 
// Copyright (c) 2020, Boia Alexandru
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are met:
// 
// 1. Redistributions of source code must retain the above copyright notice, this
//    list of conditions and the following disclaimer.
// 
// 2. Redistributions in binary form must reproduce the above copyright notice,
//    this list of conditions and the following disclaimer in the documentation
//    and/or other materials provided with the distribution.
// 
// 3. Neither the name of the copyright holder nor the names of its
//    contributors may be used to endorse or promote products derived from
//    this software without specific prior written permission.
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
// DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
// FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
// DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
// SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
// CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
// OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 
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
				mMetrics.Add( metric.Id, metric.Copy() );
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
