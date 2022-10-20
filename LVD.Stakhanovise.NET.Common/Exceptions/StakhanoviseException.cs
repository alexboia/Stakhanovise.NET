using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Exceptions
{
	public class StakhanoviseException : Exception
	{
		public StakhanoviseException( string message )
			: base( message )
		{
			return;
		}
	}
}
