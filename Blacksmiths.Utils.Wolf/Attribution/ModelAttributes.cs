using System;

namespace Blacksmiths.Utils.Wolf.Attribution
{
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

        internal bool IsValid()
        {
            if (null == this.ParentFieldNames)
                this.ParentFieldNames = new string[0];
            if (null == this.ChildFieldNames)
                this.ChildFieldNames = new string[0];

            return this.ParentFieldNames.Length == this.ChildFieldNames.Length;
        }
    }

    /// <summary>
    /// Provides a custom source column name for the type mapping. To use this attribute, register the declaring type with ColumnAttributeTypeMap.SetTypeMap&lt;T&gt;.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class ColumnAttribute : Attribute
    {
        public string Name { get; }
        public ColumnAttribute(string name)
        {
            this.Name = name;
        }
    }
}
