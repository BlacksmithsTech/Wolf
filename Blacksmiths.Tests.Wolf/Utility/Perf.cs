/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Blacksmiths.Tests.Wolf.Utility
{
	public static class Perf
	{
		public static long Measure(Action a, string desc, int iterations, int? limit = null)
		{
			var sw = new Stopwatch();
			var totalMs = limit.GetValueOrDefault() * iterations;
			long avg = 0;
			for (int i = 0; i < iterations; i++)
			{
				sw.Restart();
				a();
				avg += sw.ElapsedMilliseconds;

				if (limit.HasValue)
					Assert.IsTrue(avg < totalMs);
			}
			sw.Stop();

			if (iterations > 0)
			{
				avg = avg / iterations;
				Trace.WriteLine($"{avg}ms average - {desc} (x{iterations})");
			}

			return avg;
		}
	}
}
