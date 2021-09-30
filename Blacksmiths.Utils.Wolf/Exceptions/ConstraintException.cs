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
					sb.AppendLine($"[{Utility.DataTableHelpers.GetNormalisedName(dt)}] {row.RowError} at {row.RowState.ToString().ToLower()} row {Utility.DataRowHelpers.GetRowValues(row)}");
			}
			return sb.ToString();
		}

		
	}
}
