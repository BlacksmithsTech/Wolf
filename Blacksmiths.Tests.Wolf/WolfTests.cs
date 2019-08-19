using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using Blacksmiths.Tests.Wolf.Models;
using System.Diagnostics;
using Blacksmiths.Utils.Wolf;

namespace Blacksmiths.Tests.Wolf
{
	[TestClass]
	public class SqlServerTests
	{
		internal const string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=AdventureWorks2016;Integrated Security=true";
		private Utils.Wolf.DataConnection Connection = Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection(ConnectionString);

		[TestMethod]
		public void Request_Fetch_LooseSproc_LooseDs()
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
		public void Request_Fetch_StrongSproc_LooseDs()
		{
			var Result = Connection.NewRequest()
				.Add(
					new Sprocs.uspGetManagerEmployees() {
						BusinessEntityID = 2
					}
				)
				.Execute()
				.ToDataSet();

			Assert.IsTrue(1 == Result.Tables.Count);
			Assert.IsTrue(Result.Tables[0].Rows.Count > 0);
		}

		[TestMethod]
		public void Request_Fetch_StrongSproc_StrongDs()
		{
			var ds = new Schema.HumanResources();
			var Result = Connection.NewRequest()
				.Add(new Sprocs.uspGetDepartments(), ds.Department)
				.Execute()
				.ToDataSet(ds);

			Assert.IsTrue(1 == Result.Tables.Count);
			Assert.IsTrue(Result.Department.Count > 0);
		}

		[TestMethod]
		public void Request_SimpleModel_New_StrongSproc()
		{
			var Result = Connection.NewRequest()
				.Add(
					new Sprocs.uspGetManagerEmployees()
					{
						BusinessEntityID = 2
					}
				)
				.Execute()
				.ToSimpleModel<Models.uspGetManagerEmployeesManuallyWritten>().Results;

			//Assert.IsTrue(Result.Length > 0);
			Assert.IsNotNull(Result[0].ManagerFirstName); //property test
			Assert.IsNotNull(Result[0].ManagerLastName); //field test
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
		public void Request_LooseSp()
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
		public void Request_StrongSp()
		{
			Assert.AreEqual(5, new Sprocs.uspAdd()
			{
				Value1 = 2,
				Value2 = 3,
			}.Execute(Connection)
			.ToStoredProcedure()
			.Calculation);
		}

		[TestMethod]
		public void Request_StrongSpViaRequest()
		{
			var sp = new Sprocs.uspAdd()
			{
				Value1 = 5,
				Value2 = 6,
			};
			Connection.NewRequest().Add(sp).Execute();
			Assert.AreEqual(11, sp.Calculation);
		}

		[TestMethod]
		public void GetSchema_LooseTyping()
		{
			var rows = new Test[] { new Test(1,"Alice"), new Test(2, "Bob") };
			var ds = Connection.WithModel(rows).ToDataSet();

			Assert.AreEqual(1, ds.Tables.Count);
			Assert.IsTrue(null != ds.Tables["Test"]);
			Assert.AreEqual("Alice", ds.Tables["Test"].Rows[0]["Name"]);
		}

		[TestMethod]
		public void GetSchema_StrongTyping()
		{
			var rows = new Test[] { new Test(1, "Alice"), new Test(2, "Bob") };
			var ds = new Schema.TestData();
			Connection.WithModel(rows).MergeInto(ds);

			Assert.IsTrue(null != ds.Test);
			Assert.AreEqual("Alice", ds.Test[0].Name);
		}

		[TestMethod]
		public void GetSchema_Empty()
		{
			// ** null with no model type
			//Assert.AreEqual(0, Connection.WithModel(null).ToDataSet().Tables.Count);
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
		public void Commit_SimpleModel()
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
		public void Commit_DataSet()
		{
			var ds = new Schema.TestData();
			var r = ds.Test.NewTestRow();
			r.ID = 1;
			r.Name = "Sample original value";
			ds.Test.AddTestRow(r);
			ds.AcceptChanges();
			ds.Test[0].Name = "Sample updated value";

			Assert.AreEqual(1, Connection.WithModel(ds).AsUpdate().Commit().AffectedRowCount);
		}

		public class TestModel : Utils.Wolf.Model.ResultModel
		{

		}

		[TestMethod]
		public void Commit_Empty_Model()
		{
			var model = new TestModel();
			Assert.AreEqual(0, Connection.WithModel(model).Commit().AffectedRowCount);
		}
	}
}
