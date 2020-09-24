using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Setup
{
	public interface IConnectionSetup
	{
		IConnectionSetup WithConnectionString ( string connectionString );

		IConnectionSetup WithConnectionRetryCount (int connectionRetryCount );

		IConnectionSetup WithConnectionRetryDelayMilliseconds ( int connectionRetryDelayMilliseconds );

		IConnectionSetup WithConnectionKeepAlive ( int connectionKeepAlive );
	}
}
