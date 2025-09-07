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
		public int? Index { get; private set; }

		public QualifiedSqlName(string Schema, string Name, string index = null)
		{
			this.Schema = Schema;
			if (string.IsNullOrEmpty(this.Schema))
				this.Schema = DEFAULT_SCHEMA;
			this.Name = Name;
			if (null != index && int.TryParse(index, out var i))
				this.Index = i;
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
			var m = Regex.Match(fqName, @"^(\[?(?<schema>\w+)\]?\.)?\[?(?<name>\w+)\]?(\[(?<index>\d+)\])?$");
			if (m.Success)
				return new QualifiedSqlName(m.Groups["schema"].Value ?? string.Empty, m.Groups["name"].Value ?? fqName, m.Groups["index"].Value);
			else
				return new QualifiedSqlName(string.Empty, fqName);
		}

		public override string ToString()
		{
			return this.Index.HasValue ? $"[{Schema}].[{Name}][{Index}]" : $"[{Schema}].[{Name}]";
		}

        public bool Equals(QualifiedSqlName other)
        {
			return null != other
				&& other.Schema.Equals(this.Schema)
				&& other.Name.Equals(this.Name)
				&& Nullable.Equals(this.Index, other.Index);
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
