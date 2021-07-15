using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Processor
{
	public class StaticProcessIdProvider : IProcessIdProvider
	{
		private string mProcessId;

		public StaticProcessIdProvider( string processId )
		{
			if ( string.IsNullOrWhiteSpace( processId ) )
				throw new ArgumentNullException( nameof( processId ) );

			mProcessId = processId;
		}

		public Task SetupAsync()
		{
			return Task.CompletedTask;
		}

		public string GetProcessId()
		{
			return mProcessId;
		}
	}
}
