using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace LVD.Stakhanovise.NET.Helpers
{
	public static class NativeMethods
	{
		[DllImport( "kernel32.dll" )]
		public static extern bool QueryPerformanceCounter ( out long value );

		[DllImport( "kernel32.dll" )]
		public static extern bool QueryPerformanceFrequency ( out long value );
	}
}
