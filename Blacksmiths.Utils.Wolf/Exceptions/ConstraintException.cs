using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Exceptions
{
	public sealed class ConstraintException : WolfException
	{
		public ConstraintException(DataSet ds, Exception innerException)
			: base(AnalyseDataset(ds), innerException) { }

		private static string AnalyseDataset(DataSet ds)
		{
			var sb = new StringBuilder();
			foreach (DataTable dt in ds.Tables)
			{
				var rowErrors = dt.GetErrors();
				foreach (var row in rowErrors)
					sb.AppendLine($"[{Utility.DataTableHelpers.GetNormalisedName(dt)}] {row.RowError} at {row.RowState.ToString().ToLower()} row {GetRowValues(row)}");
			}
			return sb.ToString();
		}

		internal static string GetRowValues(DataRow row)
		{
			DataRowVersion v = DataRowVersion.Default;
			if (row.RowState == DataRowState.Deleted)
				v = DataRowVersion.Original;
			var values = new List<object>();
			foreach (DataColumn column in row.Table.Columns)
				values.Add(row[column, v]);
			return string.Join(", ", values);
		}
	}
}
