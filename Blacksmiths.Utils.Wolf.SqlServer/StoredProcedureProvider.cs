/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Data.Common;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.SqlServer
{
	public class StoredProcedureProvider : IStoredProcedureProvider
	{
		public Utility.WolfCommandBinding ToDbCommand(StoredProcedure sp, System.Data.Common.DbConnection connection)
		{
			var cmd = connection.CreateCommand();
			cmd.CommandText = sp.ProcedureName;
			cmd.CommandType = System.Data.CommandType.StoredProcedure;

			var boundParams = sp.Select(p => this.ToDbParameter(p, cmd)).ToArray();
			cmd.Parameters.AddRange(boundParams.Select(bp => bp.DbParameter).ToArray());

			return new Utility.WolfCommandBinding()
			{
				DbCommand = cmd,
				Parameters = boundParams,
				WolfRequestItem = sp
			};
		}

		public Utility.WolfParameterDbBinding ToDbParameter(StoredProcedure.SpParameter p, DbCommand command)
		{
			var dbp = command.CreateParameter();
			dbp.ParameterName = p.Name;
			dbp.Value = null != p.Value ? p.Value : DBNull.Value;
			dbp.Direction = p.Direction;
			dbp.Size = p.Length;
			if (p.ValueType.HasValue)
				dbp.DbType = p.ValueType.Value;
			else if (null == p.Value)
				throw new InvalidOperationException($"A database value type of parameter '{p.Name}' was not defined and a type could not be automatically determined.");

			return new Utility.WolfParameterDbBinding()
			{
				DbParameter = dbp,
				WolfParameter = p
			};
		}
	}
}
