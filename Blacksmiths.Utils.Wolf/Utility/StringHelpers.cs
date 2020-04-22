/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Blacksmiths.Utils.Wolf.Utility
{
	public sealed class QualifiedSqlName
	{
		private const string DEFAULT_SCHEMA = "dbo";

		public string Schema { get; private set; }
		public string Name { get; private set; }

		public QualifiedSqlName(string Schema, string Name)
		{
			this.Schema = Schema;
			if (string.IsNullOrEmpty(this.Schema))
				this.Schema = DEFAULT_SCHEMA;
			this.Name = Name;
		}

		public override string ToString()
		{
			return $"[{Schema}].[{Name}]";
		}
	}
	public static class StringHelpers
	{
		public static QualifiedSqlName GetQualifiedSqlName(string Name)
		{
			var m = Regex.Match(Name, @"^(?:\[?(?<schema>[^\n\r\[\]]+)]?\.)*\[?(?<name>[^\n\r\[\]]+)]?$");
			if (m.Success)
				return new QualifiedSqlName(m.Groups["schema"].Value ?? string.Empty, m.Groups["name"].Value ?? Name);
			else
				return new QualifiedSqlName(string.Empty, Name);
		}

		public static string GetFullTableName(System.Data.DataTable dt)
		{
			if (!string.IsNullOrEmpty(dt.Namespace))
				return $"{dt.Namespace}.{dt.TableName}";
			else
				return dt.TableName;
		}

		public static string GetFullColumnName(System.Data.DataColumn dc)
		{
			return $"{GetFullTableName(dc.Table)}.{dc.ColumnName}";
		}

        internal static string GetFullMemberName(Utility.MemberAccessor ma)
        {
            return GetFullMemberName(ma.Member);
        }

        public static string GetFullMemberName(System.Reflection.MemberInfo mi)
		{
			return $"{mi.DeclaringType.FullName}.{mi.Name}";
		}
	}
}
