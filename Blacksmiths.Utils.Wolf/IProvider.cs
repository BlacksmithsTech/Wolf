/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Blacksmiths.Utils.Wolf.Model;
using Dapper;
using System;
using System.Data.Common;
using System.Threading;

namespace Blacksmiths.Utils.Wolf
{
	/// <summary>
	/// Handler responsible for receiving changes to a row that occured during a commit. Usually for identity columns.
	/// </summary>
	/// <param name="row"></param>
	/// <param name="model"></param>
	public delegate void IdentitySyncAction(System.Data.DataRow row);

	public interface IProvider
	{
        System.Data.Common.DbConnectionStringBuilder ConnectionString { get; }
        string DatabaseName { get; }
        string Server { get; }

        IConnectionProvider GetConnectionProvider();
		IStoredProcedureProvider GetStoredProcedureProvider();
		System.Data.Common.DbDataAdapter GetDataAdapter(System.Data.Common.DbCommand selectCommand);
		System.Data.Common.DbDataAdapter GetDataAdapter(System.Data.DataTable sourceTable, System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction);
		System.Data.Common.DbCommandBuilder GetCommandBuilder(System.Data.Common.DbDataAdapter adapter);
		void EnableIdentityColumnSyncing(DbDataAdapter dbAdapter, DbConnection connection, DbTransaction transaction, string identityColumnName, IdentitySyncAction identitySyncAction);
		bool isRetriedError(System.Data.Common.DbException dbException);
    }

    public interface IConnectionProvider
	{
		System.Data.Common.DbConnection ToDbConnection();
	}

	public interface IStoredProcedureProvider
	{
		CommandDefinition ToCommandDefinition(StoredProcedure sp, DbConnection connection, DbTransaction transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default);
	}

	public interface IDataRequestCommand
	{
        CommandDefinition ToCommandDefinition(IProvider provider, DbConnection connection, DbTransaction transaction, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default);
	}
}
