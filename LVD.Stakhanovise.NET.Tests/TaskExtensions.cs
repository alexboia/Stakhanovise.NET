using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Tests
{
	public static class TaskExtensions
	{
		public static void WithoutAwait ( this Task task )
		{
			return;
		}
	}
}
