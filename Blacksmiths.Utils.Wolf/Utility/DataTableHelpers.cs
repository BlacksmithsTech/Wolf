using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Utility
{
    public static class DataTableHelpers
    {
        public static bool ContainsByNormalisedName(DataSet ds, string name)
        {
            return null != GetByNormalisedName(ds, name);
        }

        public static DataTable GetByNormalisedName(DataSet ds, string name)
        {
            // ** Exact match first
            if (ds.Tables.Contains(name))
                return ds.Tables[name];

            // ** Now normalise all ds table names
            var normalisedName = StringHelpers.GetQualifiedSqlName(name).ToString();

            foreach (DataTable dt in ds.Tables)
                if (StringHelpers.GetQualifiedSqlName(dt.TableName).ToString().Equals(normalisedName))
                    return dt;

            return null;
        }
    }
}
