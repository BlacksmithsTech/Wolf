using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Blacksmiths.Utils.Wolf.Utility
{
	internal static class PerfDebuggers
	{
		public static Dictionary<string, Stopwatch> Debuggers;

		static PerfDebuggers()
		{
#if(TRACE)
			Debuggers = new Dictionary<string, Stopwatch>();
#endif
		}

		[Conditional("TRACE")]
		public static void BeginTrace(string Name)
		{
			var sw = new Stopwatch();
			sw.Start();
			Debuggers.Add(Name, sw);
		}

		[Conditional("TRACE")]
		public static void EndTrace(string Name)
		{
			Trace.WriteLine($"{Name} {Debuggers[Name].ElapsedMilliseconds}ms");
			Debuggers[Name].Stop();
			Debuggers.Remove(Name);
		}
	}
}
