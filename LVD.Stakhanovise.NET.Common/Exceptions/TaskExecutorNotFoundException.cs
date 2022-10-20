using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Exceptions
{
	public class TaskExecutorNotFoundException : StakhanoviseException
	{
		public TaskExecutorNotFoundException( string payloadTypeName )
			: base( "No executor found for task" )
		{
			PayloadTypeName = payloadTypeName;
		}

		public string PayloadTypeName
		{
			get; private set;
		}
	}
}
