/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
		public void Request_NotSlower_NoTracking()
		{
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

			var efAvg = Utility.Perf.Measure(() =>
			{
				efContext.Query<EF.Models.uspGetManagerEmployees>().AsNoTracking().FromSql("uspGetManagerEmployees @p0", 2).ToList();
			}, "Entity Framework", 10);

			this.AssertFasterThanEntityFramework(efAvg, wolfAvg);
		}

		[TestMethod]
		public void Request_NotSlower_WithTracking()
		{
			var efAvg = Utility.Perf.Measure(() =>
			{
				efContext.Query<EF.Models.uspGetManagerEmployees>().FromSql("uspGetManagerEmployees @p0", 2).ToList();
			}, "Entity Framework", 10);

			var wolfAvg = Utility.Perf.Measure(() =>
			{
				wolfConnection.NewRequest()
				.Add(
					new Utils.Wolf.StoredProcedure("uspGetManagerEmployees")
						.AddParameter("BusinessEntityID", 2)
				)
				.Execute()
				.ToSimpleModel<Models.uspGetManagerEmployeesManuallyWritten>();
			}, "Wolf", 10);

			this.AssertFasterThanEntityFramework(efAvg, wolfAvg);
		}

		[TestMethod]
		public void Request_NotSlower_2()
		{
			var efAvg = Utility.Perf.Measure(() =>
			{
				efContext = new EF.Models.AdventureWorks2016Context();
				efContext.Set<EF.Models.BusinessEntity>().FromSql("uspGetBusinessEntities").Load();
				efContext.Set<EF.Models.BusinessEntityAddress>().FromSql("uspGetBusinessEntityAddresses").Load();

				//var Ents = efContext.BusinessEntity.ToList();
				//var i = 0;
				//foreach(var e in Ents)
				//	foreach(var a in e.BusinessEntityAddress)
				//	{
				//		i++;
				//	}

			}, "Entity Framework", 10);

			//var wolfAvg = Utility.Perf.Measure(() =>
			//{
			//	wolfConnection.NewRequest()
			//	.Add(
			//		new Utils.Wolf.StoredProcedure("uspGetManagerEmployees")
			//			.AddParameter("BusinessEntityID", 2)
			//	)
			//	.Execute()
			//	.ToSimpleModel<Models.uspGetManagerEmployeesManuallyWritten>();
			//}, "Wolf", 10);

			//this.AssertFasterThanEntityFramework(efAvg, wolfAvg);
		}

		private void AssertFasterThanEntityFramework(long efAvg, long wolfAvg)
		{
			if (wolfAvg < efAvg)
				Trace.WriteLine($"Wolf was {efAvg - wolfAvg}ms faster on average");
			else if (wolfAvg == efAvg)
				Trace.WriteLine("Identical performance");
			else
				Trace.WriteLine($"Wolf was {wolfAvg - efAvg}ms slower on average");
			Assert.IsTrue(wolfAvg <= efAvg);
		}
	}
}
