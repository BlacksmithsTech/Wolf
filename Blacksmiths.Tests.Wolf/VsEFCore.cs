﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using Blacksmiths.Tests.Wolf.Models;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Blacksmiths.Tests.Wolf
{
	[TestClass]
	public class VsEFCore
	{
		private EF.Models.AdventureWorks2016Context efContext = new EF.Models.AdventureWorks2016Context();
		private Utils.Wolf.DataConnection wolfConnection = Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection(SqlServerTests.ConnectionString);

		[TestMethod]
		public void Request_NotSlower()
		{
			var efAvg = Utility.Perf.Measure(() =>
			{
				efContext.Query<EF.Models.uspGetManagerEmployees>().AsNoTracking().FromSql("uspGetManagerEmployees @p0", 2).ToList();
			}, "Entity Framework", 10);

			var wolfAvg = Utility.Perf.Measure(() =>
			{
				wolfConnection.NewRequest()
				.Add(
					new Utils.Wolf.StoredProcedure("uspGetManagerEmployees")
						.AddParameter("BusinessEntityID", 2)
				)
				.Execute()
				.ToDataSet();

			}, "Wolf", 10);

			this.AssertFasterThanEntityFramework(efAvg, wolfAvg);
		}

		private void AssertFasterThanEntityFramework(long efAvg, long wolfAvg)
		{
			Assert.IsTrue(wolfAvg <= efAvg);
			if (wolfAvg < efAvg)
				Trace.WriteLine($"Wolf was {efAvg - wolfAvg}ms faster on average");
			else if (wolfAvg == efAvg)
				Trace.WriteLine("Identical performance");
			else
				Trace.WriteLine($"Wolf was {wolfAvg - efAvg}ms slower on average");
		}
	}
}