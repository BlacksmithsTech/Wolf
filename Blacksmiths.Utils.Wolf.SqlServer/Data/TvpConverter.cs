using Blacksmiths.Utils.Wolf.Utility;
using Microsoft.Data.SqlClient;
using Microsoft.Data.SqlClient.Server;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmiths.Utils.Wolf.SqlServer.Data
{
    public class TvpConverter
    {
        public sealed record TvpColumn(
            int Ordinal,
            SqlMetaData MetaData,
            MemberAccessor Accessor
            );

        public UdtTableAttribute Udt { get; }
        public Type Type { get; set; }
        public TvpColumn[] Columns { get; }

        public TvpConverter(Type udtType)
        {
            var attrTable = udtType.GetCustomAttribute<UdtTableAttribute>();
            if (null == attrTable)
                throw new InvalidOperationException($"{udtType} is not a user defined type. Annotate it with [UdtTable]");

            this.Type = udtType;
            this.Udt = attrTable;
            this.Columns = this.InspectType(udtType);
        }

        public IEnumerable<SqlDataRecord> ToRecords(IEnumerable<object> values)
        {
            var metaData = this.Columns.Select(c => c.MetaData).ToArray();

            foreach(var value in values)
            {
                if (value.GetType() != this.Type)
                    throw new ArgumentException($"All objects converting to TVP records must be of type '{this.Type}'");

                var record = new SqlDataRecord(metaData);
                foreach (var column in this.Columns)
                {
                    var v = column.Accessor.GetValue(value);
                    if (null != v)
                        record.SetValue(column.Ordinal, v);
                    else
                        record.SetDBNull(column.Ordinal);
                }
                yield return record;
            }
        }

        private TvpColumn[] InspectType(Type udtType)
        {
            var result = new List<TvpColumn>();
            var props = udtType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in props)
            {
                var attrColumn = prop.GetCustomAttribute<UdtColumnAttribute>();
                if (null == attrColumn)
                    continue;
                result.Add(new TvpColumn(attrColumn.Ordinal, this.CreateSqlMetaData(prop, attrColumn), MemberAccessor.Create(prop)));
            }

            return result.ToArray();
        }

        private SqlMetaData CreateSqlMetaData(PropertyInfo prop, UdtColumnAttribute attrColumn)
        {
            //var dbType = attrColumn.SqlDbType.HasValue ? attrColumn.SqlDbType.Value : this.MapSqlDbType(StoredProcedure.SpParameter.InferValueType(prop.PropertyType));
            var dbType = this.MapSqlDbType(StoredProcedure.SpParameter.InferValueType(prop.PropertyType));

            var columnName = attrColumn.Name ?? prop.Name;

            switch (dbType)
            {
                case SqlDbType.NVarChar:
                    if (0 == attrColumn.Length)
                        throw new InvalidOperationException($"NVARCHAR type '{columnName}' did not specify a field length");
                    return new SqlMetaData(columnName, dbType, attrColumn.Length);

                default:
                    return new SqlMetaData(columnName, dbType);
            }
        }

        private SqlDbType MapSqlDbType(DbType dbType)
        {
            return new SqlParameter() { DbType = dbType }.SqlDbType;
        }
    }
}
