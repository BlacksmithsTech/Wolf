using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using Blacksmiths.Tests.Wolf.Models;
using System.Diagnostics;

namespace Blacksmiths.Tests.Wolf
{
	[TestClass]
	public class SqlServerTests
	{
		internal const string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=AdventureWorks2016;Integrated Security=true";
		private Utils.Wolf.DataConnection Connection = Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection(ConnectionString);

		[TestMethod]
		public void Request_Fetch1()
		{
			var Result = Connection.NewRequest()
				.Add(
					new Utils.Wolf.StoredProcedure("uspGetManagerEmployees")
						.AddParameter("BusinessEntityID", 2)
				)
				.Execute()
				.ToDataSet();

			Assert.IsTrue(1 == Result.Tables.Count);
			Assert.IsTrue(Result.Tables[0].Rows.Count > 0);
		}

		[TestMethod]
		public void Request_Fetch1_Perf()
		{
			Utility.Perf.Measure(() => {
				var Result = Connection.NewRequest()
					.Add(
						new Utils.Wolf.StoredProcedure("uspGetManagerEmployees")
							.AddParameter("BusinessEntityID", 2)
					)
					.Execute()
					.ToDataSet();
			}, "uspGetManagerEmployees", 10, 100);
		}

		

		[TestMethod]
		public void Request_SpOnly()
		{
			Assert.AreEqual(5, new Utils.Wolf.StoredProcedure("uspAdd")
						.AddParameter("Value1", 2)
						.AddParameter("Value2", 3)
						.AddOutputParameter<int>("Calculation")
						.Execute(Connection)
						.ToStoredProcedure()
						.GetParameterValue<int>("Calculation"));
		}

		[TestMethod]
		public void GetSchema()
		{
			var rows = new Test[] { new Test(1,"Alice"), new Test(2, "Bob") };
			var ds = Connection.WithModel(rows).ToDataSet();

			Assert.AreEqual(1, ds.Tables.Count);
			Assert.IsTrue(null != ds.Tables["Test"]);
			Assert.AreEqual("Alice", ds.Tables["Test"].Rows[0]["Name"]);
		}

		[TestMethod]
		public void GetSchema_Empty()
		{
			// ** null with no model type
			Assert.AreEqual(0, Connection.WithModel(null).ToDataSet().Tables.Count);
			// ** null with a model type
			Test[] NullCollection = null;
			this.GetSchema_Empty_Assert(Connection.WithModel(NullCollection).ToDataSet());
			// ** Empty array
			this.GetSchema_Empty_Assert(Connection.WithModel(new Test[0]).ToDataSet());
			// ** all null items
			Test[] NullItemCollection = new Test[] { null };
			this.GetSchema_Empty_Assert(Connection.WithModel(NullItemCollection).ToDataSet());
		}

		private void GetSchema_Empty_Assert(DataSet ds)
		{
			Assert.AreEqual(1, ds.Tables.Count);
			Assert.IsTrue(null != ds.Tables["Test"]);
			Assert.IsTrue(0 == ds.Tables["Test"].Rows.Count);
		}

		[TestMethod]
		public void Commit_Fluent()
		{
			var rows = new Test[] { new Test(1, "Alice"), new Test(2, "Bob") };
			var ds = Connection.WithModel(rows).AsUpdate().Commit();
		}

		[TestMethod]
		public void Commit_Empty()
		{
			Assert.AreEqual(0, Connection.WithModel(new Test[0]).Commit().AffectedRowCount);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentException))]
		public void Commit_Anon_NoMeta()
		{
			var ds = Connection.WithModel(new[] {
				new { ID = 1, Name = "Alexander Ali" },
			})
				.Commit();
		}
	}
}
