﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Utility
{
    public static class DataTableHelpers
    {
        private const string C_EXTENDED_WOLF_IDENTITY = "_WolfIdentity";

        //public static bool ContainsByNormalisedName(DataSet ds, string name)
        //{
        //    return null != GetByNormalisedName(ds, name);
        //}

        public static DataTable GetByNormalisedName(DataSet ds, string name)
        {
            // ** Exact name match first
            if (ds.Tables.Contains(name))
                return ds.Tables[name];

            var fqName = QualifiedSqlName.Parse(name);

            // ** Namespaced match
            if (ds.Tables.Contains(fqName.Name, fqName.Schema))
                return ds.Tables[fqName.Name, fqName.Schema];

            // ** Now normalise all ds table names
            var normalisedName =fqName.ToString();

            foreach (DataTable dt in ds.Tables)
                if (QualifiedSqlName.Parse(dt.TableName).ToString().Equals(normalisedName))
                    return dt;

            return null;
        }

        public static void MarkIdentityColumn(DataColumn col)
        {
            if (!col.ExtendedProperties.Contains(C_EXTENDED_WOLF_IDENTITY))
                col.ExtendedProperties.Add(C_EXTENDED_WOLF_IDENTITY, true);
        }

        public static DataColumn GetIdentityColumn(DataTable dt)
        {
            foreach (DataColumn col in dt.Columns)
                if (col.ExtendedProperties.Contains(C_EXTENDED_WOLF_IDENTITY) && (bool)col.ExtendedProperties[C_EXTENDED_WOLF_IDENTITY])
                    return col;
            return null;
        }

        public static ForeignKeyConstraint GetForeignKey(DataTable dt, string Key, string[] parentColumns, string[] childColumns)
        {
            foreach(var constraint in dt.Constraints)
                if(constraint is ForeignKeyConstraint fk)
                {
                    if (parentColumns.All(c => fk.Columns.Any(pc => pc.ColumnName.Equals(c))) && childColumns.All(childCol => fk.RelatedColumns.Any(rc => rc.ColumnName.Equals(childCol))))
                        return fk;
                }
            return null;
        }
    }
}
