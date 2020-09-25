using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Executors
{
	public enum DependencyScope
	{
		Singleton = 0x01,
		Thread = 0x02,
		Transient = 0x03
	}
}
