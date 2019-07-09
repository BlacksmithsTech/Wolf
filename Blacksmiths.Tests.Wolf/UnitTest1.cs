using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blacksmiths.Tests.Wolf
{
	[TestClass]
	public class SqlServerTests
	{
		const string ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=AdventureWorks2016;Integrated Security=true";

		[TestMethod]
		public void Request_Fluent_Fetch1()
		{
			var Connection = Blacksmiths.Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection(ConnectionString);

			var Result = Connection.NewRequest()
				.Add(
					new Utils.Wolf.StoredProcedure("uspGetManagerEmployees")
						.AddParameter("BusinessEntityID", 2)
				)
				.Execute()
				.ToDataSet();
		}

		[TestMethod]
		public void Request_Fluent_SpOnly()
		{
			var Connection = Blacksmiths.Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection(ConnectionString);
			Assert.AreEqual(new Utils.Wolf.StoredProcedure("uspAdd")
						.AddParameter("Value1", 2)
						.AddParameter("Value2", 3)
						.AddOutputParameter<int>("Calculation")
						.Execute(Connection)
						.ToStoredProcedure()
						.GetParameterValue<int>("Calculation"), 5);
		}
	}
}
