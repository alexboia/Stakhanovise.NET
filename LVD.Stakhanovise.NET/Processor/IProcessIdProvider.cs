using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public interface IProcessIdProvider
	{
		Task SetupAsync();
		
		string GetProcessId();
	}
}
