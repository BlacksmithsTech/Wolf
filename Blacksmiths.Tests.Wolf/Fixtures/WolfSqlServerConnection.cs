using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.SqlServer;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmiths.Tests.Wolf.Fixtures
{
    public abstract class WolfSqlServerConnection
    {
        internal DataConnection Connection { get; }

        public WolfSqlServerConnection(string connectionString)
        {
            this.Connection = SqlServerProvider.NewSqlServerConnection(connectionString);
        }
    }

    public class WolfTestDatabaseSqlServerConnection : WolfSqlServerConnection
    {
        public WolfTestDatabaseSqlServerConnection()
            : base(TestConfiguration.Configuration.GetConnectionString("WolfTests")) { }
    }
}
