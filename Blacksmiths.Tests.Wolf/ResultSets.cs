using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.Attribution;
using System.Data.Common;

namespace Blacksmiths.Tests.Wolf
{
    [Collection(Collections.Constants.WolfTestDatabase)]
    public class ResultSets
    {
        [Procedure(Name = "[dbo].[spStoredProcedure]")]
        private class spStoredProcedure_MissingParams : StoredProcedure
        {
        }

        [Procedure(Name = "[dbo].[spStoredProcedure]")]
        private class spStoredProcedure : StoredProcedure
        {
            public string ValueInStr { get; set; }
            public DateTimeOffset? ValueInDt { get; set; }
            public bool? ValueInBit { get; set; }
            public string OptionalValueStr { get; set; }

            [Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
            public string ValueOutStr { get; set; }

            [Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
            public DateTimeOffset? ValueOutDt { get; set; }
        }

        [Procedure(Name = "[dbo].[spMultiResult]")]
        private class spMultiResult : StoredProcedure
        {
        }

        private class spMultiResult0
        {
            public string Column1 { get; set; }
            public int Column2 { get; set; }
        }

        private class spMultiResult1
        {
            public string Column1 { get; set; }
            public DateTimeOffset Column2 { get; set; }
            public int Column3 { get; set; }
        }

        private class spMultiResultModel
        {
            public spMultiResult0[] Table0 { get; set; }
            public spMultiResult1[] Table1 { get; set; }
        }

        private readonly IDataConnection connection;
        public ResultSets(Fixtures.WolfTestDatabaseSqlServerConnection wolfTestDatabaseSqlServerConnection)
        {
            this.connection = wolfTestDatabaseSqlServerConnection.Connection;
        }

        [Fact]
        public async Task Execute_MissingParams()
        {
            await Assert.ThrowsAsync<DbException>(async () => {
                await this.connection.NewRequest()
                    .Add(new spStoredProcedure_MissingParams())
                    .ExecuteAsync();
            });
        }

        [Fact]
        public async Task Execute_StoredProcedure()
        {
            var now = DateTimeOffset.UtcNow;
            var sproc = new spStoredProcedure() { ValueInStr = "Hello world", ValueInDt = now };
            var resultCode = await this.connection.NewRequest()
                .Add(sproc)
                .ExecuteAsync();

            Assert.Equal(99, resultCode);
            Assert.Equal("Hello world output", sproc.ValueOutStr);
            Assert.Equal(now.AddDays(1), sproc.ValueOutDt);
        }

        [Fact]
        public async Task Multi_Stream()
        {
            using var connection = this.connection.NewRequest()
                .Add(new spMultiResult())
                .OpenConnection();
            var r1 = (await connection.StreamNextResultSetAsync<spMultiResult0>()).ToList();
            var r2 = (await connection.StreamNextResultSetAsync<spMultiResult1>()).ToList();
        }
    }
}
