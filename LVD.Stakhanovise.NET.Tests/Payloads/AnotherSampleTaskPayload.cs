using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Tests.Payloads
{
	public class AnotherSampleTaskPayload
	{
		public AnotherSampleTaskPayload ()
		{
			return;
		}

		public AnotherSampleTaskPayload ( string text )
		{
			Text = text;
		}

		public string Text { get; set; }
	}
}
