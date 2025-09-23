/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Blacksmiths.Utils.Wolf.SqlServer.Data;
using Blacksmiths.Utils.Wolf.Utility;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Blacksmiths.Utils.Wolf.SqlServer
{
	public class StoredProcedureProvider : IStoredProcedureProvider
	{
		public CommandDefinition ToCommandDefinition(StoredProcedure sp, DbConnection connection, DbTransaction transaction = null, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
			=> new CommandDefinition(((IStoredProcedure)sp).ProcedureName, ((IStoredProcedure)sp).GetDynamicParameters(), transaction, commandTimeoutSeconds, CommandType.StoredProcedure,
                flags: CommandFlags.None,
                cancellationToken: cancellationToken);
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
			dbp.Size = this.Length ?? -1;
			dbp.SqlDbType = this.ValueTypeSql;
			dbp.TypeName = this.TvpQualifiedName;
            return dbp;
        }
    }

	public sealed class BoundSpParameterSqlServerTvp : SpParameterSqlServerTvp
    {
        private object _instance;
        private System.Reflection.PropertyInfo _property;
		private Type _memberType;
		private TvpConverter _tvpConverter;

        public override object Value
        {
            get { return this._tvpConverter.ToRecords((object[])this._property.GetValue(this._instance)); }
			set
			{ 
				// Intentionally blank. UDTs are read only.
			}
        }

        public BoundSpParameterSqlServerTvp(string name, object instance, PropertyInfo property)
			: base(name, null)
		{
            this._instance = instance;
            this._property = property;

			// Get the member type (not its array or collection)
			//this._memberType = ReflectionHelper.GetMemberType(property);
			this._memberType = ReflectionHelper.GetMemberCollectionType(property);
			if (null == this._memberType)
				throw new ArgumentException($"Property '{property}' must be a collection to use as a table valued parameter");

			this._tvpConverter = new TvpConverter(this._memberType);
			this.TvpQualifiedName = this._tvpConverter.Udt.Name;
        }
	}
}
