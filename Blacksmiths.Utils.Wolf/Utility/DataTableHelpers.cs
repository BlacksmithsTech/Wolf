using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Utility
{
	public static class DataTableHelpers
	{
		private const string C_EXTENDED_WOLF_IDENTITY = "_WolfIdentity";
		private const string C_EXTENDED_WOLF_TARGET = "_WolfTarget";

		//public static bool ContainsByNormalisedName(DataSet ds, string name)
		//{
		//    return null != GetByNormalisedName(ds, name);
		//}

		public static string GetNormalisedName(DataTable dt)
		{
			return QualifiedSqlName.From(dt).ToString();
		}

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
			var normalisedName = fqName.ToString();

			foreach (DataTable dt in ds.Tables)
				if (QualifiedSqlName.Parse(dt.TableName).ToString().Equals(normalisedName))
					return dt;

			return null;
		}

		public static void SetTarget(DataTable dt, Attribution.Target target)
		{
			if (!dt.ExtendedProperties.Contains(C_EXTENDED_WOLF_TARGET))
				dt.ExtendedProperties.Add(C_EXTENDED_WOLF_TARGET, target);
		}

		public static Attribution.Target GetTarget(DataTable dt)
		{
			if (dt.ExtendedProperties.Contains(C_EXTENDED_WOLF_TARGET))
				return dt.ExtendedProperties[C_EXTENDED_WOLF_TARGET] as Attribution.Target;
			else
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
			foreach (var constraint in dt.Constraints)
				if (constraint is ForeignKeyConstraint fk)
				{
					if (parentColumns.All(c => fk.Columns.Any(pc => pc.ColumnName.Equals(c))) && childColumns.All(childCol => fk.RelatedColumns.Any(rc => rc.ColumnName.Equals(childCol))))
						return fk;
				}
			return null;
		}

		internal static bool HasChanges(DataTable table)
		{
			return table.Rows.Cast<DataRow>().Any(dr => dr.RowState != DataRowState.Unchanged);
		}
	}

	internal sealed class DataTableComparer : IComparer<DataTable>
	{
		public int Compare(DataTable x, DataTable y)
		{
			if (this.IsChildOfY(x, y))
				return 1;//x > y
			else if (this.IsParentOfY(x, y))
				return -1;//x < y
						  //else if (x.ParentRelations.Count > 0)
						  //    return -1;
						  //else if (x.ChildRelations.Count > 0)
						  //    return 1;

			return 0;
		}

		private bool IsChildOfY(DataTable x, DataTable y)
		{
			var parentRelationships = x.ParentRelations.Cast<DataRelation>();
			foreach (var relation in parentRelationships)
			{
				if (y == relation.ParentTable)
					return true;
				else if (relation.ParentTable == relation.ChildTable)
					return false;

				var ParentComparison = this.IsChildOfY(relation.ParentTable, y);
				if (ParentComparison)
					return ParentComparison;
			}

			return false;
		}

		private bool IsParentOfY(DataTable x, DataTable y)
		{
			foreach (var relation in x.ChildRelations.Cast<DataRelation>())
			{
				if (y == relation.ChildTable)
					return true;
				else if (relation.ParentTable == relation.ChildTable)
					return false;

				var ChildComparison = this.IsParentOfY(relation.ChildTable, y);
				if (ChildComparison)
					return ChildComparison;
			}

			return false;
		}
	}

	internal sealed class DataRowComparer : IComparer<DataRow>
	{
		public int Compare(DataRow x, DataRow y)
		{
			if (x.RowState == DataRowState.Added)
			{
				if (y.RowState == DataRowState.Modified)
					return -1;//x < y
				else if (y.RowState == DataRowState.Deleted)
					return -1;//x < y
				else if(y.RowState == DataRowState.Added)
					return this.CompareOrderSensitive(x, y, DataRowVersion.Current);
			}
			else if (x.RowState == DataRowState.Modified)
			{
				if (y.RowState == DataRowState.Added)
					return 1;//x > y
				else if (y.RowState == DataRowState.Deleted)
					return -1;//x < y
			}
			else if (x.RowState == DataRowState.Deleted)
			{
				if (y.RowState == DataRowState.Added)
					return 1;//x > y
				else if (y.RowState == DataRowState.Modified)
					return 1;//x > y
				else if (y.RowState == DataRowState.Deleted)
					return this.CompareOrderSensitive(x, y, DataRowVersion.Original) * -1;
			}
			return 0;

		}

		private int CompareOrderSensitive(DataRow x, DataRow y, DataRowVersion version)
		{
			if (this.IsDescendantOfY(x, y, version))
				return 1;
			else if (this.IsAncestorOfY(x, y, version))
				return -1;//x < y
			return 0;// x == y
		}

		private bool IsDescendantOfY(DataRow x, DataRow y, DataRowVersion version)
		{
			var sameTableRelationships = this.GetParentRelationships(x);

			foreach (var relationship in sameTableRelationships)
			{
				var parentRows = new List<DataRow>();
				var parentRow = x.GetParentRow(relationship, version);
				while (null != parentRow && !parentRows.Contains(parentRow))
				{
					if (y == parentRow)
						return true;

					parentRows.Add(parentRow);
					parentRow = parentRow.GetParentRow(relationship, DataRowVersion.Original);
				}
			}
			return false;
		}

		private bool IsAncestorOfY(DataRow x, DataRow y, DataRowVersion version)
		{
			var sameTableRelationships = this.GetChildRelationships(x);
			foreach (var relationship in sameTableRelationships)
			{
				var childRows = new List<DataRow>();
				var childRowsHere = x.GetChildRows(relationship, version);
				while (null != childRowsHere && childRowsHere.Length > 0 && !childRowsHere.Any(cr => childRows.Contains(cr)))
				{
					if (childRowsHere.Contains(y))
						return true;

					childRows.AddRange(childRowsHere);
					var newChildRows = new List<DataRow>();
					foreach (var row in childRowsHere)
						newChildRows.AddRange(row.GetChildRows(relationship, version));
					childRowsHere = newChildRows.Where(r => r.RowState != DataRowState.Deleted).ToArray();
				}
			}
			return false;
		}

		private IEnumerable<DataRelation> GetParentRelationships(DataRow x) => x.Table.ParentRelations.Cast<DataRelation>().Where(dr => dr.ParentTable == x.Table);

		private IEnumerable<DataRelation> GetChildRelationships(DataRow x) => x.Table.ChildRelations.Cast<DataRelation>().Where(dr => dr.ChildTable == x.Table);

	}
}
