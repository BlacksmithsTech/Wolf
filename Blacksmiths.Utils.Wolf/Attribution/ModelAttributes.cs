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
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]//TODO: Targets for properties and fields
	public class Target : Attribute
	{
		/// <summary>
		/// For classes specify the exact name of table in your database where data should be stored.
		/// </summary>
		public string To { get; set; }
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
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class Relation : Attribute
	{
		public string[] ParentFieldNames { get; set; }
		public string[] ChildFieldNames { get; set; }

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
			var parentColumns = parentModelLink.GetAllMembers(this.ParentFieldNames);
			var childColumns = childModelLink.GetAllMembers(this.ChildFieldNames);
			if (parentColumns.Any() && childColumns.Any())
			{
				var constraint = new System.Data.ForeignKeyConstraint(parentColumns.Select(pc => pc.Column).ToArray(), childColumns.Select(cc => cc.Column).ToArray());
				constraint.AcceptRejectRule = System.Data.AcceptRejectRule.None;
				childModelLink.Data.Constraints.Add(constraint);
				return constraint;
			}
			else
			{
				return null;
			}
		}
	}
}
