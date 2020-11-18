using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IStakhanoviseSetupDefaultsProvider
	{
		StakhanoviseSetupDefaults GetDefaults ();
	}
}
