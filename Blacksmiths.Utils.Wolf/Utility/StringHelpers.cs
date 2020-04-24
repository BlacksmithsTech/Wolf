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

		public static QualifiedSqlName From(System.Data.DataTable dt)
		{
			var fqName = QualifiedSqlName.Parse(dt.TableName);
			if (!string.IsNullOrEmpty(dt.Namespace) && !dt.Namespace.StartsWith("http:", StringComparison.CurrentCultureIgnoreCase))
				fqName.Schema = dt.Namespace;
			return fqName;
		}

		public static QualifiedSqlName Parse(string fqName)
		{
			var m = Regex.Match(fqName, @"^(?:\[?(?<schema>[^\n\r\[\]]+)]?\.)*\[?(?<name>[^\n\r\[\]]+)]?$");
			if (m.Success)
				return new QualifiedSqlName(m.Groups["schema"].Value ?? string.Empty, m.Groups["name"].Value ?? fqName);
			else
				return new QualifiedSqlName(string.Empty, fqName);
		}

		public override string ToString()
		{
			return $"[{Schema}].[{Name}]";
		}

		public string ToDisplayString()
		{
			if (DEFAULT_SCHEMA.Equals(this.Schema))
				return this.Name;
			else
				return $"{this.Schema}.{this.Name}";
		}
	}

	public static class StringHelpers
	{
		public static string GetFullColumnName(System.Data.DataColumn dc)
		{
			return $"{QualifiedSqlName.From(dc.Table).ToDisplayString()}.{dc.ColumnName}";
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
