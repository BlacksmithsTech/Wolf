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
			var warmUpIterations = iterations > 0 ? 1 : 0;
			for (int i = 0; i < iterations + warmUpIterations; i++)
			{
				sw.Restart();
				a();
				var elapsed = sw.ElapsedMilliseconds;
				if (i < warmUpIterations)
				{
					Trace.WriteLine($"Warm up {i + 1}: {elapsed}ms");
				}
				else
				{
					var displayIterations = i - warmUpIterations + 1;
					Trace.WriteLine($"{displayIterations}: {elapsed}ms");
					avg += elapsed;

					if (limit.HasValue && avg > totalMs)
					{
						avg = avg / Math.Max(displayIterations, 1);
						Assert.Fail($"Performance limit {limit}ms exceeded - {avg}ms average - {desc} (x{displayIterations}/{iterations})");
					}
				}
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
