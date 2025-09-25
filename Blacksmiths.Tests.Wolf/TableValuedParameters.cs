using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.Attribution;
using Blacksmiths.Utils.Wolf.SqlServer;
using Blacksmiths.Utils.Wolf.SqlServer.Data;
using Dapper;
using Microsoft.Data.SqlClient.Server;
using System.Data;

namespace Blacksmiths.Tests.Wolf
{
    [Collection(Collections.Constants.WolfTestDatabase)]
    public class TableValuedParameters
    {
        [Procedure(Name = "[dbo].[spEchoTVP]")]
        private class spEchoTVPArray : StoredProcedure
        {
            [Parameter(Provider = typeof(BoundSpParameterSqlServerTvp))]
            public udtSample[] Input { get; set; }
        }

        [Procedure(Name = "[dbo].[spEchoTVP]")]
        private class spEchoTVPEnumerable : StoredProcedure
        {
            [Parameter(Provider = typeof(BoundSpParameterSqlServerTvp))]
            public IEnumerable<udtSample> Input { get; set; }
        }

        [Procedure(Name = "[dbo].[spEchoTVP]")]
        private class spEchoTVPList : StoredProcedure
        {
            [Parameter(Provider = typeof(BoundSpParameterSqlServerTvp))]
            public List<udtSample> Input { get; set; }
        }

        [UdtTable(Name = "[dbo].[udtSample]")]
        private class udtSample
        {
            [UdtColumn(0)]
            public Guid Id { get; set; }

            [UdtColumn(1, Length = 100)]
            public string Name { get; set; }

            [UdtColumn(2)]
            public int? Age { get; set; }
        }

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
                CreateSampleSqlDataRecord(udtMetaData, Guid.NewGuid(), "David", null),
            };

            var result = await this.connection.NewRequest()
                .Add(new StoredProcedure("[dbo].[spEchoTVP]")
                    .AddParameter(new SpParameterSqlServerTvp("Input", "[dbo].[udtSample]", System.Data.ParameterDirection.Input, udtData))
                )
                .QueryAsync<udtSample>();

            // Test the echo is identical
            Assert.Equal(result.Count, udtData.Length);
        }

        [Fact]
        public async Task Echo_StrongTyping_ReturnsIdentical()
        {
            var udtData = GetSampleData();

            var result = await this.connection.NewRequest()
                .Add(new spEchoTVPArray() { Input = udtData })
                .QueryAsync<udtSample>();

            Assert.Equal(result.Count, udtData.Length);

            var result2 = await this.connection.NewRequest()
                .Add(new spEchoTVPEnumerable() { Input = udtData })
                .QueryAsync<udtSample>();

            Assert.Equal(result2.Count, udtData.Length);

            var result3 = await this.connection.NewRequest()
                .Add(new spEchoTVPList() { Input = new List<udtSample>(udtData) })
                .QueryAsync<udtSample>();

            Assert.Equal(result3.Count, udtData.Length);
        }

        private udtSample[] GetSampleData() => new udtSample[]
        {
            new udtSample() { Id = Guid.NewGuid(), Name = "Jacob", Age = 30 },
            new udtSample() { Id = Guid.NewGuid(), Name = "Sarah", Age = 40 },
            new udtSample() { Id = Guid.NewGuid(), Name = "Sophie", Age = 24 },
            new udtSample() { Id = Guid.NewGuid(), Name = "David" },
        };

        private object NullSafe(object obj) => obj is DBNull ? null : obj;

        private SqlDataRecord CreateSampleSqlDataRecord(SqlMetaData[] metaData, Guid id, string name, int? age)
        {
            var r = new SqlDataRecord(metaData);
            r.SetGuid(0, id);
            r.SetString(1, name);
            if (age.HasValue)
                r.SetInt32(2, age.Value);
            return r;
        }
    }
}


