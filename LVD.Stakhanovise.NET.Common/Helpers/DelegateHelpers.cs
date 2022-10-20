using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class DelegateHelpers
	{
		public static Action<T> CreateNoOpAction<T>()
		{
			return ( t ) => { };
		}
	}
}
