using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Attribution
{
	/// <summary>
	/// Configures the source of data population for the given class, property or field. This attribute can be used multiple times if there are multiple possible alternative sources.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class Source : Attribute
	{
		/// <summary>
		/// For classes specify the exact name of a request item used to fetch the data. 
		/// For non-collection properties and fields, specify the exact name of a column to populate the member.
		/// For collections can be used to override the source of data used to populate the member.
		/// </summary>
		public string From { get; set; }
	}

	/// <summary>
	/// Configures the target of where on the database data should be persisted for the given class.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]//TODO: Targets for properties and fields
	public class Target : Attribute
	{
		/// <summary>
		/// For classes specify the exact name of table in your database where data should be stored.
		/// For collections, specify the exact name of the table in your database where data should be stored.
		/// 
		/// This approach should be used when Wolf is responsible for generating CRUD database commands.
		/// </summary>
		public string To { get; set; }

		/// <summary>
		/// Specifies the type of a stored procedure that can be used for inserts
		/// </summary>
		public Type InsertUsing { get; set; }

		/// <summary>
		/// Specifies the type of a stored procedure that can be used for updates
		/// </summary>
		public Type UpdateUsing { get; set; }

		/// <summary>
		/// Specifies the type of a stored procedure that can be used for deletes
		/// </summary>
		public Type DeleteUsing { get; set; }

		internal bool CommitUsingSprocs => null != this.InsertUsing || null != this.UpdateUsing || null != this.DeleteUsing;
	}

	/// <summary>
	/// Defines that a property or field is a key
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class Key : Attribute
	{
		public enum KeyType
		{
			PrimaryKey,
		}

		public KeyType Type { get; set; } = KeyType.PrimaryKey;
	}

	/// <summary>
	/// Defines database constraints for the given property or field
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class Constraint : Attribute
	{
		public bool Nullable { get; set; } = true;
		public int Length { get; set; } = -1;
	}

	/// <summary>
	/// Configures the relationship of a nested collection or object
	/// 
	/// Apply to a property or field to create a cross table relationship.
	/// Apply to a class to apply a same table relationship.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
	public class Relation : Attribute
	{
		public string[] ParentFieldNames { get; set; }
		public string[] ChildFieldNames { get; set; }
		public bool isParentTable { get; set; } = true;

		public Relation(string SharedParentChildFieldName)
			: this(SharedParentChildFieldName, SharedParentChildFieldName) { }

		public Relation(string ParentFieldName, string ChildFieldName)
		{
			this.ParentFieldNames = new[] { ParentFieldName };
			this.ChildFieldNames = new[] { ChildFieldName };
		}

		public Relation(string[] ParentFieldNames, string[] ChildFieldNames)
		{
			this.ParentFieldNames = ParentFieldNames;
			this.ChildFieldNames = ChildFieldNames;
		}

		internal bool IsSane()
		{
			if (null == this.ParentFieldNames)
				this.ParentFieldNames = new string[0];
			if (null == this.ChildFieldNames)
				this.ChildFieldNames = new string[0];

			return this.ParentFieldNames.Length == this.ChildFieldNames.Length;
		}

		internal System.Data.ForeignKeyConstraint CreateForeignKey(Model.ModelLink parentModelLink, Model.ModelLink childModelLink)
		{
			var parentColumns = parentModelLink.GetAllMembers(this.ParentFieldNames).ToArray();
			var childColumns = childModelLink.GetAllMembers(this.ChildFieldNames).ToArray();
			if (parentColumns.Any() && childColumns.Any())
			{
				// ** Relationship and constraints
				System.Data.ForeignKeyConstraint constraint;
				System.Data.DataTable data;

				if (this.isParentTable)
				{
					data = childModelLink.Data;
					constraint = new System.Data.ForeignKeyConstraint(parentColumns.Select(pc => pc.Column).ToArray(), childColumns.Select(cc => cc.Column).ToArray());
				}
				else
				{
					data = parentModelLink.Data;
					constraint = new System.Data.ForeignKeyConstraint(childColumns.Select(cc => cc.Column).ToArray(), parentColumns.Select(pc => pc.Column).ToArray());
				}

				constraint.AcceptRejectRule = System.Data.AcceptRejectRule.None;

				data.Constraints.Add(constraint);
				data.DataSet.Relations.Add(constraint.RelatedColumns, constraint.Columns);

				//if (this.isParentTable)
				//else
				//	data.DataSet.Relations.Add(constraint.Columns, constraint.RelatedColumns);

				return constraint;
			}
			else
			{
				return null;
			}
		}
	}

	/// <summary>
	/// Specifies that the given enum value will be used as a fallback if the database value cannot be deserialised (for example, when application is older than database)
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
	public sealed class FallbackEnum : Attribute
	{
	}
}
