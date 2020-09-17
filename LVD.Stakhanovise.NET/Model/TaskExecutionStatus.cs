using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Model
{
	public enum TaskExecutionStatus
	{
		ExecutedSuccessfully = 0x01,
		ExecutionCancelled = 0x02,
		ExecutedWithError = 0x03
	}
}
