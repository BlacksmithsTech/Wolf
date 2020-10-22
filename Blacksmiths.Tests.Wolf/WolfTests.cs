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
using Blacksmiths.Utils.Wolf;
using System.Linq;

namespace Blacksmiths.Tests.Wolf
{
    /// <summary>
    /// Development test handle - intended as a sandpit for experimenting with the Wolf component rather than as unit tests
    /// </summary>
	[TestClass]
	public class SqlServerTests
	{
		internal const string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=AdventureWorks2016;Integrated Security=true";
		private Utils.Wolf.DataConnection Connection = Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection(ConnectionString);
		private DateTime now = DateTime.Now;

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
				.Add(new Sprocs.uspGetDepartments(), ds._HumanResources_Department)
				.Execute()
				.ToDataSet(ds);

			Assert.IsTrue(1 == Result.Tables.Count);
			Assert.IsTrue(Result._HumanResources_Department.Count > 0);
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
		public void Request_SimpleModel_New_StrongSproc_NoResults()
		{
			var Result = Connection.NewRequest()
				.Add(
					new Sprocs.uspGetManagerEmployees()
					{
						BusinessEntityID = 444 //no results for this
					}
				)
				.Execute()
				.ToSimpleModel<Models.uspGetManagerEmployeesManuallyWritten>().Results;

			Assert.IsNotNull(Result);
			Assert.IsTrue(Result.Count == 0);
		}

		[TestMethod]
		public void Request_SimpleModel_New_DoesntCorrelate_StrongSproc()
		{
			// When the sprocs used don't correlate to the model, we should expect null
			Assert.IsNull(Connection.NewRequest()
				.Add(new Sprocs.uspGetBusinessEntityAddresses())
				.Execute()
				.ToSimpleModel<Models.BusinessEntityNoRelationshipAttribution>().Results);
		}

		[TestMethod]
		public void Request_SimpleModel_New_UnsuppliedNested_StrongSproc()
		{
			// This model requests a nested model, but it's not supplied in the request. All the nested models should be null
			var IncompleteResult = Connection.NewRequest()
				.Add(new Sprocs.uspGetBusinessEntities())
				.Execute()
				.ToSimpleModel<Models.BusinessEntityNoRelationshipAttribution>().Results;

			Assert.IsTrue(IncompleteResult.All(r => null == r.BusinessEntityAddresses));
		}

		[TestMethod]
		public void Request_SimpleModel_New_CrossNested_StrongSproc()
		{
			var Result = Connection.NewRequest()
				.Add(new Sprocs.uspGetBusinessEntities())
				.Add(new Sprocs.uspGetBusinessEntityAddresses())
				.Execute()
				.ToSimpleModel<Models.BusinessEntityNoRelationshipAttribution>().Results;

			var First = Result.First();
            Assert.IsTrue(Result.All(r => r.BusinessEntityAddresses.Length == First.BusinessEntityAddresses.Length));
		}

		[TestMethod]
		public void Request_SimpleModel_New_Related_Array_StrongSproc()
		{
			var Result = Connection.NewRequest()
				.Add(new Sprocs.uspGetBusinessEntities())
				.Add(new Sprocs.uspGetBusinessEntityAddresses())
				.Execute()
				.ToSimpleModel<Models.BusinessEntity>().Results;
			Assert.IsNotNull(Result);
			Assert.IsTrue(Result.All(r => null != r.BusinessEntityAddresses));
		}

		[TestMethod]
		public Utils.Wolf.Model.SimpleResultModel<Models.BusinessEntityList> Request_SimpleModel_New_Related_List_StrongSproc()
		{
			var model = Connection.NewRequest()
				.Add(new Sprocs.uspGetBusinessEntities())
				.Add(new Sprocs.uspGetBusinessEntityAddresses())
				.Execute()
				.ToSimpleModel<Models.BusinessEntityList>();
			var Result = model.Results;
			Assert.IsNotNull(Result);
			Assert.IsTrue(Result.All(r => null != r.BusinessEntityAddresses));
			return model;
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
		public void Request_Fetch2_Perf()
		{
			Utility.Perf.Measure(() => {
				var Result = Connection.NewRequest()
				.Add(new Sprocs.uspGetBusinessEntities())
				.Add(new Sprocs.uspGetBusinessEntityAddresses())
				.Execute();
			}, "uspGetBusinessEntities, uspGetBusinessEntityAddresses (No processing)", 10, 100);
		}

		[TestMethod]
		public void Request_Fetch3_Perf()
		{
			Utility.Perf.Measure(() => {
				var Result = Connection.NewRequest()
				.Add(new Sprocs.uspGetBusinessEntities())
				.Add(new Sprocs.uspGetBusinessEntityAddresses())
				.Execute()
				.ToDataSet();
			}, "uspGetBusinessEntities, uspGetBusinessEntityAddresses (DataSet)", 10, 100);
		}

		[TestMethod]
		public void Request_Fetch4_Perf()
		{
			// 200ms currently alloted for 40,391 rows
			Utility.Perf.Measure(() => {
				var Result = Connection.NewRequest()
				.Add(new Sprocs.uspGetBusinessEntities())
				.Add(new Sprocs.uspGetBusinessEntityAddresses())
				.Execute()
				.ToSimpleModel<Models.BusinessEntityNoRelationshipAttribution>().Results;
			}, "uspGetBusinessEntities, uspGetBusinessEntityAddresses (Boxed, Unrelated)", 10, 200);
		}

        [TestMethod]
        public void Request_Fetch5_Perf()
        {
            // 300ms currently alloted for 60,005 rows
            Utility.Perf.Measure(() => {
                var Result = this.GetEntities().Results;
            }, "uspGetBusinessEntities, uspGetBusinessEntityAddresses, uspGetPersonAddress (Boxed, Related)", 10, 300);
        }

		[TestMethod]
		public void Request_Fetch_Transaction()
		{
			var result = Connection.NewRequest(options =>
			{
				options.UseTransaction = true;
			})
				.Add(new Sprocs.uspGetBusinessEntities())
				.Add(new Sprocs.uspGetBusinessEntityAddresses())
				.Execute();
		}

		[TestMethod]
		public void Commit_SimpleModel()
		{
			//var rows = new Test[] { new Test(1, "Alice"), new Test(2, "Bob") };
			//var result1 = Connection.WithModel(rows).AsUpdate().Commit();
			//Assert.AreEqual(rows.Length, result1.AffectedRowCount);
			var newRow = new Test() { Name = $"New single object row created at {now}" };
			var result2 = Connection.WithModel(newRow).Commit();
			Assert.AreEqual(1, result2.AffectedRowCount);
			Assert.IsTrue(newRow.ID > 0);
		}

		[TestMethod]
        public void Commit_NestedModel()
        {
			//var Model = this.GetEntities();
			//Model.Results[0].BusinessEntityAddresses[0].Address.AddressLine1 = "123 Fake Street";
			//var ds = Connection.WithModel(Model).ToDataSet().GetChanges();

			// ** Insert new category
			var Cat = new TestCat();
			Cat.MyId = 0;
			Cat.Name = $"New category object row created at {now}";
			Cat.Records.Add(new Test() { Name = $"Child of cat created at {now}", Category = 0 });
			Cat.Records.Add(new Test() { Name = $"Child 2 of cat created at {now}", Category = 0 });
			Connection.WithModel(Cat).Commit();
			Assert.IsTrue(Cat.MyId > 0);
			Assert.IsTrue(Cat.Records[0].ID > 0);
			Assert.IsTrue(Cat.Records[0].Category == Cat.MyId);
		}

		[TestMethod]
		public void Commit_NestedModel_Update()
		{
			var model = Request_SimpleModel_New_Related_List_StrongSproc();
			model.Results[0].ModifiedDate = DateTime.Now;
			Connection.WithModel(model).Commit();
		}

		private Utils.Wolf.Model.SimpleResultModel<Models.BusinessEntity> GetEntities()
        {
            return Connection.NewRequest()
                .Add(new Sprocs.uspGetBusinessEntities())
                .Add(new Sprocs.uspGetBusinessEntityAddresses())
                .Add(new Sprocs.uspGetPersonAddress())
                .Execute()
                .ToSimpleModel<Models.BusinessEntity>();
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
            Connection.WithModel(rows).ToDataSet(ds);

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
			Assert.IsNull(ds.GetChanges());
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

        [TestMethod]
        public void Model_OneToOne()
        {
            var Model = new Schema.TestData();
            var Parent = Model.GroupOfTests.AddGroupOfTestsRow(1);
            const string Value = "Hello World";
            Model.GroupExtras.AddGroupExtrasRow(Parent, Value);
            var Result = Connection.WithModel(Model).ToSimpleModel<GroupOfTests>().Results;
            Assert.AreEqual(Value, Result?.FirstOrDefault().Extras?.Name);
        }
	}
}
