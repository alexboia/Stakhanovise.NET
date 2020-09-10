using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Payloads
{
	public class SampleTaskPayload
	{
		public SampleTaskPayload ()
		{
			return;
		}

		public SampleTaskPayload ( int counter )
		{
			Counter = counter;
		}

		public int Counter { get; set; }
	}
}
