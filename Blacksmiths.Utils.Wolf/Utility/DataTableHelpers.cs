using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Utility
{
    public static class DataTableHelpers
    {
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
    }
}
