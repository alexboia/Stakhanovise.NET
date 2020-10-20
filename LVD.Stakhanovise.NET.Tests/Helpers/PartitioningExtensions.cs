using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Helpers
{
	public static class PartitioningExtensions
	{
		public static int[] PartitionValue ( this int value, int count )
		{
			int i = 0;
			int[] partitions = new int[ count ];

			while ( value > 0 )
			{
				partitions[ i++ % count ] += 1;
				value--;
			}

			return partitions;
		}
	}
}
