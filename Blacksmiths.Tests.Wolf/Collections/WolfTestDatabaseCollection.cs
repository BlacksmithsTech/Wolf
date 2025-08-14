using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmiths.Tests.Wolf.Collections
{
    [CollectionDefinition(Constants.WolfTestDatabase)]
    public class WolfTestDatabaseCollection : ICollectionFixture<Fixtures.WolfTestDatabaseSqlServerConnection>
    {
    }
}
