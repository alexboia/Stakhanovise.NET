using System;

namespace LVD.Stakhanovise.NET.Management.Model
{
	public class StakhanoviseInstance
	{
		public Guid Id
		{
			get; set;
		}

		public string Name
		{
			get; set;
		}

		public string ConnectionString
		{
			get; set;
		}

		public StakhanoviseInstanceProperties Properties
		{
			get; set;
		}
	}
}
