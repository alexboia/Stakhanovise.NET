using System;
using System.Collections.Generic;
using System.Text;

namespace LVD.Stakhanovise.NET.Logging.Tests.Harness
{
	public class TargetLoggerMockLoggerSetupProperties
	{
		public static TargetLoggerMockLoggerSetupProperties DebugEnabledWithoutExpectations()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsDebugEnabled = true,
				DebugMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties DebugEnabledWithExpectations( LogMessageExpectations expectations )
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsDebugEnabled = true,
				DebugMessageExpectations = expectations
			};
		}

		public static TargetLoggerMockLoggerSetupProperties DebugDisabled()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsDebugEnabled = false,
				DebugMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties ErrorEnabledWithoutExpectations()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsErrorEnabled = true,
				ErrorMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties ErrorDisabled()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsErrorEnabled = false,
				ErrorMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties ErrorEnabledWithExpectations( LogMessageExpectations expectations )
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsErrorEnabled = true,
				ErrorMessageExpectations = expectations
			};
		}

		public static TargetLoggerMockLoggerSetupProperties FatalEnabledWithoutExpectations()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsFatalEnabled = true,
				FatalMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties FatalDisabled()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsFatalEnabled = false,
				FatalMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties FatalEnabledWithExpectations( LogMessageExpectations expectations )
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsFatalEnabled = true,
				FatalMessageExpectations = expectations
			};
		}

		public static TargetLoggerMockLoggerSetupProperties InfoEnabledWithoutExpectations()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsInfoEnabled = true,
				InfoMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties InfoDisabled()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsInfoEnabled = false,
				InfoMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties InfoEnabledWithExpectations( LogMessageExpectations expectations )
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsInfoEnabled = true,
				InfoMessageExpectations = expectations
			};
		}

		public static TargetLoggerMockLoggerSetupProperties TraceEnabledWithoutExpectations()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsTraceEnabled = true,
				TraceMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties TraceDisabled()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsTraceEnabled = false,
				TraceMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties TraceEnabledWithExpectations( LogMessageExpectations expectations )
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsTraceEnabled = true,
				TraceMessageExpectations = expectations
			};
		}

		public static TargetLoggerMockLoggerSetupProperties WarnEnabledWithoutExpectations()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsWarnEnabled = true,
				WarnMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties WarnDisabled()
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsWarnEnabled = false,
				WarnMessageExpectations = null
			};
		}

		public static TargetLoggerMockLoggerSetupProperties WarnEnabledWithExpectations( LogMessageExpectations expectations )
		{
			return new TargetLoggerMockLoggerSetupProperties()
			{
				IsWarnEnabled = true,
				WarnMessageExpectations = expectations
			};
		}

		public bool IsDebugEnabled
		{
			get; set;
		}

		public LogMessageExpectations DebugMessageExpectations
		{
			get; set;
		}

		public bool IsErrorEnabled
		{
			get; set;
		}

		public LogMessageExpectations ErrorMessageExpectations
		{
			get; set;
		}

		public bool IsFatalEnabled
		{
			get; set;
		}

		public LogMessageExpectations FatalMessageExpectations
		{
			get; set;
		}

		public bool IsInfoEnabled
		{
			get; set;
		}

		public LogMessageExpectations InfoMessageExpectations
		{
			get; set;
		}

		public bool IsTraceEnabled
		{
			get; set;
		}

		public LogMessageExpectations TraceMessageExpectations
		{
			get; set;
		}

		public bool IsWarnEnabled
		{
			get; set;
		}

		public LogMessageExpectations WarnMessageExpectations
		{
			get; set;
		}
	}
}
