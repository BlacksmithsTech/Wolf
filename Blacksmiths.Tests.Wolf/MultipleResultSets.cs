using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.Attribution;
using Blacksmiths.Utils.Wolf.SqlServer;
using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmiths.Tests.Wolf
{
    [Collection(Collections.Constants.WolfTestDatabase)]
    public class MultipleResultSets
    {
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
        public MultipleResultSets(Fixtures.WolfTestDatabaseSqlServerConnection wolfTestDatabaseSqlServerConnection)
        {
            this.connection = wolfTestDatabaseSqlServerConnection.Connection;
        }

        [Fact]
        public async Task Multi_Bind()
        {
            using var connection = this.connection.NewRequest()
                .Add(new spMultiResult())
                .OpenConnection();
            var r1 = (await connection.StreamNextResultSetAsync<spMultiResult0>()).ToList();
            var r2 = (await connection.StreamNextResultSetAsync<spMultiResult1>()).ToList();
        }
    }
}
