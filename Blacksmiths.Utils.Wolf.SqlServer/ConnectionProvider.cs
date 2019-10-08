/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Data.SqlClient;

namespace Blacksmiths.Utils.Wolf.SqlServer
{
	public class ConnectionProvider : IConnectionProvider
	{
		private SqlServerProvider _provider;

		public ConnectionProvider(SqlServerProvider provider)
		{
			this._provider = provider;

		}
		public DbConnection ToDbConnection()
		{
			return new SqlConnection(this._provider.ConnectionString.ToString());
		}
	}
}
