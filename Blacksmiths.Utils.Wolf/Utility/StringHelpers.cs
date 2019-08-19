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
	public static class StringHelpers
	{
		public static (string Schema, string Name) GetQualifiedSpName(string Name)
		{
			var m = Regex.Match(Name, @"^(?:\[(?<schema>[^\n\r\[\]]+)]\.)*\[(?<name>[^\n\r\[\]]+)]$");
			if (m.Success)
				return (m.Groups["schema"].Value ?? string.Empty, m.Groups["name"].Value ?? Name);
			else
				return (string.Empty, Name);
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

		public static string GetFullMemberName(System.Reflection.MemberInfo mi)
		{
			return $"{mi.DeclaringType.FullName}.{mi.Name}";
		}
	}
}
