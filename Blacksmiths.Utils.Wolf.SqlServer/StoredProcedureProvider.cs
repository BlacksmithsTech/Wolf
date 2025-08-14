/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;

namespace Blacksmiths.Utils.Wolf.SqlServer
{
	public class StoredProcedureProvider : IStoredProcedureProvider
	{
		public Utility.WolfCommandBinding ToDbCommand(StoredProcedure sp, DbConnection connection, DbTransaction transaction = null)
		{
			var cmd = connection.CreateCommand();
			cmd.CommandText = sp.ProcedureName;
			cmd.CommandType = System.Data.CommandType.StoredProcedure;
			cmd.Transaction = transaction;

			var boundParams = sp.Select(p => this.ToDbParameter(p, cmd)).ToArray();
			cmd.Parameters.AddRange(boundParams.Select(bp => bp.DbParameter).ToArray());

			return new Utility.WolfCommandBinding()
			{
				DbCommand = cmd,
				Parameters = boundParams,
				WolfRequestItem = sp
			};
		}

		public Utility.WolfParameterDbBinding ToDbParameter(StoredProcedure.ISpParameter p, DbCommand command)
		{
			return new Utility.WolfParameterDbBinding()
			{
				DbParameter = p.ToDbParameter(command),
				WolfParameter = p
			};
		}
	}

	//interface ISpParameterSqlServer
	//{
	//	SqlDbType ValueTypeSql { get; set; }
	//}

	public class SpParameterSqlServerTvp : StoredProcedure.SpParameter//, ISpParameterSqlServer
	{
		/// <summary>
		/// Gets or sets the name of the SQL server user table-type representing the TVP
		/// </summary>
		public string TvpQualifiedName { get; set; }
		public SqlDbType ValueTypeSql { get; } = SqlDbType.Structured;

        public SpParameterSqlServerTvp(string name, string tvpQualifiedName, ParameterDirection direction = ParameterDirection.Input, IEnumerable<SqlDataRecord> value = null)
			: base(name, direction, value) 
		{
			this.TvpQualifiedName = tvpQualifiedName;
		}

        public override DbParameter ToDbParameter(DbCommand command)
        {
            var dbp = (SqlParameter)command.CreateParameter();
            dbp.ParameterName = this.Name;
            dbp.Value = null != this.Value ? this.Value : DBNull.Value;
            dbp.Direction = this.Direction;
            dbp.Size = this.Length;
			dbp.SqlDbType = this.ValueTypeSql;
			dbp.TypeName = this.TvpQualifiedName;
            return dbp;
        }
    }

	public sealed class BoundSpParameterSqlServerTvp : SpParameterSqlServerTvp
    {
        private object _instance;
        private System.Reflection.PropertyInfo _property;

        public override object Value
        {
            get { return this._property.GetValue(this._instance, null); }
            set { this._property.SetValue(this._instance, value, null); }
        }

        public BoundSpParameterSqlServerTvp(string name, string tvpQualifiedName, object instance, PropertyInfo property)
			: base(name, tvpQualifiedName)
		{
            this._instance = instance;
            this._property = property;
        }
	}
}
