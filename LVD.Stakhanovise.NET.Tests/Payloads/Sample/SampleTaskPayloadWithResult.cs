using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Payloads
{
	public class SampleTaskPayloadWithResult
	{
		public SampleTaskPayloadWithResult()
		{
			return;
		}

		public SampleTaskPayloadWithResult( int counter )
		{
			Counter = counter;
		}

		public int Counter
		{
			get; set;
		}
	}
}
