using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Model
{
	public class ComputeFactorialResult
	{
		public ComputeFactorialResult ( int forN, long result )
		{
			ForN = forN;
			Result = result;
		}

		public int ForN { get; set; }

		public long Result { get; set; }
	}
}
