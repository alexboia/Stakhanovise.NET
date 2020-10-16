using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Payloads
{
	public class ComputeFactorial
	{
		public ComputeFactorial ()
		{
			return;
		}

		public ComputeFactorial ( int forN )
		{
			ForN = forN;
		}

		public int ForN { get; set; }
	}
}
