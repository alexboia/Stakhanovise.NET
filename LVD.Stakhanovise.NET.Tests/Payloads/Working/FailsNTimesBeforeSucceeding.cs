using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Payloads.Working
{
	public class FailsNTimesBeforeSucceeding
	{
		public FailsNTimesBeforeSucceeding ( Guid id, int failuresBeforeSuccess )
		{
			Id = id;
			FailuresBeforeSuccess = failuresBeforeSuccess;
		}

		public Guid Id { get; set; }

		public int FailuresBeforeSuccess { get; set; }
	}
}
