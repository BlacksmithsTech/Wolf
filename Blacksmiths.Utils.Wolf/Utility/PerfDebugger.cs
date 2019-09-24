using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Blacksmiths.Utils.Wolf.Utility
{
	internal static class PerfDebuggers
	{
		public static ConcurrentDictionary<string, Stopwatch> Debuggers;

		static PerfDebuggers()
		{
#if(TRACE)
			Debuggers = new ConcurrentDictionary<string, Stopwatch>();
#endif
		}

		[Conditional("TRACE")]
		public static void BeginTrace(string Name)
		{
			var sw = new Stopwatch();
			sw.Start();
			Debuggers.TryAdd(Name, sw);
		}

		[Conditional("TRACE")]
		public static void EndTrace(string Name)
		{
			Trace.WriteLine($"{Name} {Debuggers[Name].ElapsedMilliseconds}ms");
			Debuggers[Name].Stop();
            Stopwatch w;
			Debuggers.TryRemove(Name, out w);
		}
	}
}
