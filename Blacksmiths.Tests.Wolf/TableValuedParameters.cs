using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.SqlServer;
using Microsoft.Data.SqlClient.Server;
using System.Data;

namespace Blacksmiths.Tests.Wolf
{
    [Collection(Collections.Constants.WolfTestDatabase)]
    public class TableValuedParameters
    {
        private readonly IDataConnection connection;
        public TableValuedParameters(Fixtures.WolfTestDatabaseSqlServerConnection wolfTestDatabaseSqlServerConnection)
        {
            this.connection = wolfTestDatabaseSqlServerConnection.Connection;
        }

        [Fact]
        public async Task Echo_LooseTyping_ReturnsIdentical()
        {
            var udtMetaData = new SqlMetaData[]
            {
                new SqlMetaData("Id", System.Data.SqlDbType.UniqueIdentifier),
                new SqlMetaData("Name", System.Data.SqlDbType.NVarChar, 100),
                new SqlMetaData("Age", System.Data.SqlDbType.Int),
            };

            var udtData = new SqlDataRecord[]
            {
                CreateSampleSqlDataRecord(udtMetaData, Guid.NewGuid(), "Jacob", 30),
                CreateSampleSqlDataRecord(udtMetaData, Guid.NewGuid(), "Sarah", 40),
                CreateSampleSqlDataRecord(udtMetaData, Guid.NewGuid(), "Sophie", 24),
            };

            var result = await this.connection.NewRequest()
                .Add(new StoredProcedure("[dbo].[spEchoTVP]")
                    .AddParameter(new SpParameterSqlServerTvp("Input", "[dbo].[udtSample]", System.Data.ParameterDirection.Input, udtData))
                )
                .ExecuteAsync()
                .ToDataSetAsync();

            // Test the echo is identical
            var table = result.Tables[0];
            Assert.Equal(table.Rows.Count, udtData.Length);
            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
                foreach (DataColumn column in table.Columns)
                    Assert.Equal(udtData[rowIndex][column.ColumnName], table.Rows[rowIndex][column]);
        }

        private SqlDataRecord CreateSampleSqlDataRecord(SqlMetaData[] metaData, Guid id, string name, int age)
        {
            var r = new SqlDataRecord(metaData);
            r.SetGuid(0, id);
            r.SetString(1, name);
            r.SetInt32(2, age);
            return r;
        }
    }
}


