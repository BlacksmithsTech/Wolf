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
using System.Data;

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
			this.ConnectionString = this.PrepareConnectionString(connectionString);
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
		// Utility
		// *************************************************

		private string PrepareConnectionString(string cs)
		{
			var csb = new SqlConnectionStringBuilder(cs);
			if (string.IsNullOrWhiteSpace(csb.ApplicationName) || csb.ApplicationName.Equals(new SqlConnectionStringBuilder().ApplicationName))
			{
				var WolfAssembly = System.Reflection.Assembly.GetAssembly(typeof(IProvider));
				var ProgramAssembly = System.Reflection.Assembly.GetEntryAssembly();
				csb.ApplicationName = $"{this.GetProductName(WolfAssembly)} {this.GetProductVersion(WolfAssembly)} ({this.GetProductName(ProgramAssembly)} {this.GetProductVersion(ProgramAssembly)})";
			}
			return csb.ToString();
		}

		private string GetProductName(System.Reflection.Assembly a)
		{
			string ProductName = null;

			try
			{
				ProductName = System.Diagnostics.FileVersionInfo.GetVersionInfo(a.Location).ProductName;
			}
			finally
			{
				if (string.IsNullOrWhiteSpace(ProductName))
					ProductName = a.FullName;
			}

			return ProductName;
		}

		private string GetProductVersion(System.Reflection.Assembly a)
		{
			var v = a.GetName().Version;
			if (0 == v.Build && 0 == v.Revision)
				return $"{v.Major}.{v.Minor}";
			else if (0 == v.Revision)
				return $"{v.Major}.{v.Minor}.{v.Build}";
			else
				return v.ToString();
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

		public DbDataAdapter GetDataAdapter(DataTable sourceTable, DbConnection connection, DbTransaction transaction = null)
		{
			if (null == sourceTable || 0 == sourceTable.Columns.Count)
				return null;

			var selectCommand = new StringBuilder();
			selectCommand.Append("SELECT ");
			for(int i = 0; i < sourceTable.Columns.Count; i++)
			{
				selectCommand.Append($"[{sourceTable.Columns[i].ColumnName}]");
				if (i + 1 < sourceTable.Columns.Count)
					selectCommand.Append(", ");
			}
			selectCommand.Append($" FROM [{sourceTable.TableName}]");

			var cmd = connection.CreateCommand();
			cmd.CommandText = selectCommand.ToString();
			cmd.Transaction = transaction;

			return new SqlDataAdapter((SqlCommand)cmd);
		}

		public DbCommandBuilder GetCommandBuilder(DbDataAdapter adapter)
		{
			return new SqlCommandBuilder((SqlDataAdapter)adapter);
		}
	}
}
