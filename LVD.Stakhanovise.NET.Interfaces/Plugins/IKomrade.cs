using LVD.Stakhanovise.NET.Setup;
using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Plugins
{
	public interface IKomrade
	{
		void Setup( IStakhanoviseSetup setup, IKomradeApi api );
	}
}
