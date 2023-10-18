using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Blacksmiths.Utils.Wolf.Utility
{
	//
	// Summary:
	//     Defines logging severity levels.
	internal enum LogLevel
	{
		//
		// Summary:
		//     Logs that contain the most detailed messages. These messages may contain sensitive
		//     application data. These messages are disabled by default and should never be
		//     enabled in a production environment.
		Trace = 0,
		//
		// Summary:
		//     Logs that are used for interactive investigation during development. These logs
		//     should primarily contain information useful for debugging and have no long-term
		//     value.
		Debug = 1,
		//
		// Summary:
		//     Logs that track the general flow of the application. These logs should have long-term
		//     value.
		Information = 2,
		//
		// Summary:
		//     Logs that highlight an abnormal or unexpected event in the application flow,
		//     but do not otherwise cause the application execution to stop.
		Warning = 3,
		//
		// Summary:
		//     Logs that highlight when the current flow of execution is stopped due to a failure.
		//     These should indicate a failure in the current activity, not an application-wide
		//     failure.
		Error = 4,
		//
		// Summary:
		//     Logs that describe an unrecoverable application or system crash, or a catastrophic
		//     failure that requires immediate attention.
		Critical = 5,
		//
		// Summary:
		//     Not used for writing log messages. Specifies that a logging category should not
		//     write any messages.
		None = 6
	}

	internal static class Logging
	{
		internal static ConcurrentDictionary<string, Stopwatch> Debuggers;
		internal delegate void LogAction(LogLevel level, string message, params object[] args);

		internal static LogAction LogHandler { get; set; } = (level, message, args) => LogUsingTracing(message);

		static Logging()
		{
#if(TRACE)
			Debuggers = new ConcurrentDictionary<string, Stopwatch>();
#endif
		}

		[Conditional("TRACE")]
		public static void Trace(string message)
		{
			System.Diagnostics.Trace.WriteLine(message);
		}

		[Conditional("TRACE")]
		public static void BeginTrace(string Name)
		{
			var sw = new Stopwatch();
			sw.Start();
			Debuggers.TryAdd(Name, sw);//the keynames are shared across all threads
		}

		[Conditional("TRACE")]
		public static void EndTrace(string Name)
		{
			if (Debuggers.TryGetValue(Name, out var sw))
			{
				System.Diagnostics.Trace.WriteLine($"{Name} {sw.ElapsedMilliseconds}ms");
				sw.Stop();
			}
			Debuggers.TryRemove(Name, out var swr);
		}

		internal static void Log(LogLevel level, string message, params object[] args)
		{
			if (null != LogHandler)
				LogHandler(level, message, args);
		}

		public static void LogUsingTracing(string message)
		{
			System.Diagnostics.Trace.WriteLine(message);
		}

#if NETSTANDARD
		public static void LogUsingMicrosoftLogging(Microsoft.Extensions.Logging.ILogger<IDataConnection> log, LogLevel level, string message, params object[] args)
		{
			Microsoft.Extensions.Logging.LoggerExtensions.Log(log, (Microsoft.Extensions.Logging.LogLevel)level, message, args);
		}
#endif

	}
}
