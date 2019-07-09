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
	public class SqlServerProvider : IProvider
	{
		// *************************************************
		// Fields
		// *************************************************

		private StoredProcedureProvider _spProvider;
		private ConnectionProvider _connectionProvider;

		// *************************************************
		// Properties
		// *************************************************

		/// <summary>
		/// Gets the connection string used for this SQL Server connection
		/// </summary>
		public string ConnectionString { get; private set; }

		// *************************************************
		// Constructor & Factory
		// *************************************************

		public SqlServerProvider(string connectionString)
		{
			if (null == connectionString)
				throw new ArgumentNullException("connectionString may not be null");
			this.ConnectionString = connectionString;
		}

		/// <summary>
		/// Creates a new SQL Server data connection service
		/// </summary>
		/// <param name="connectionString">Connection string to use to connect to the database</param>
		/// <returns>Database connection</returns>
		public static DataConnection NewSqlServerConnection(string connectionString)
		{
			return new DataConnection(new SqlServerProvider(connectionString));
		}

		// *************************************************
		// Contract (IProvider)
		// *************************************************

		public IConnectionProvider GetConnectionProvider()
		{
			if (null == this._connectionProvider)
				this._connectionProvider = new ConnectionProvider(this);
			return this._connectionProvider;
		}

		public IStoredProcedureProvider GetStoredProcedureProvider()
		{
			if (null == this._spProvider)
				this._spProvider = new StoredProcedureProvider();
			return this._spProvider;
		}

		public DbDataAdapter GetDataAdapter(DbCommand selectCommand)
		{
			return new SqlDataAdapter((SqlCommand)selectCommand);
		}
	}
}
