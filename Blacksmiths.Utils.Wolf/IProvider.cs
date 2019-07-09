/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;

namespace Blacksmiths.Utils.Wolf
{
	public interface IProvider
	{
		IConnectionProvider GetConnectionProvider();
		IStoredProcedureProvider GetStoredProcedureProvider();
		System.Data.Common.DbDataAdapter GetDataAdapter(System.Data.Common.DbCommand selectCommand);
		System.Data.Common.DbDataAdapter GetDataAdapter(System.Data.DataTable sourceTable, System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction transaction);
		System.Data.Common.DbCommandBuilder GetCommandBuilder(System.Data.Common.DbDataAdapter adapter);
	}

	public interface IConnectionProvider
	{
		System.Data.Common.DbConnection ToDbConnection();
	}

	public interface IStoredProcedureProvider
	{
		Utility.WolfCommandBinding ToDbCommand(StoredProcedure sp, System.Data.Common.DbConnection connection);
		Utility.WolfParameterBinding ToDbParameter(StoredProcedure.Parameter p, System.Data.Common.DbCommand command);
	}

	public interface IDataRequestItem
	{
		Utility.WolfCommandBinding GetDbCommand(IProvider provider, System.Data.Common.DbConnection connection);
	}
}
