﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Data;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal interface IRelationshipDemand
	{
        void CreateForeignKey(System.Data.DataSet dataSet);
	}

    internal sealed class MemberRelationshipDemand : IRelationshipDemand
    {
        private System.Collections.IList parentCollection;

        internal ModelLink ParentModelLink { get; private set; }
        internal ModelDefinition ChildModelDefinition { get; private set; }
        internal Attribution.Relation Relationship { get; set; }

        internal MemberRelationshipDemand(ModelLink parentModelLink, ModelDefinition childModelDefinition, System.Collections.IList parentCollection)
        {
            this.ChildModelDefinition = childModelDefinition;
            this.ParentModelLink = parentModelLink;
            this.parentCollection = parentCollection;
        }

        internal void Satisfy(ModelLink childLink, System.Collections.IList sourceCollection)
        {
            if (null == sourceCollection)
            {
                // No data available to satisfy the relationship.
                return;
            }

            var Relation = childLink.FindFirstValidRelationshipWithParent(this.ParentModelLink);

            if (null != Relation)
            {
                var Lookup = sourceCollection.Cast<object>().ToLookup(k => childLink.GetKey(k, Relation.ChildFieldNames));

                foreach (var parenti in this.parentCollection)
                {
                    var ParentKey = this.ParentModelLink.GetKey(parenti, Relation.ParentFieldNames);
                    childLink.ModelDefinition.SetValue(parenti, Lookup[ParentKey]);
                }
            }
            else if(!childLink.ModelDefinition.RelationshipAttributes.Value.Any())
            {
                // ** No relationship. Cross join all children with a reference to the same collection
                //var Values = this.Relationship.MemberType.IsArray ? Utility.ReflectionHelper.ArrayFromList(this.Relationship.CollectionType, sourceCollection) : sourceCollection;
                foreach (var parenti in this.parentCollection)
                    childLink.ModelDefinition.SetValue(parenti, sourceCollection);
            }
        }

		public void CreateForeignKey(System.Data.DataSet dataSet)
		{
            var childModelLink = this.ChildModelDefinition.GetModelTarget(dataSet);
            var formalRelationship = this.Relationship ?? childModelLink.FindFirstValidRelationshipWithParent(this.ParentModelLink);

            // ** Create the DataTable relationships and constraints, and force the values to match so ADO.NET recognizes the relationship
            formalRelationship?.CreateForeignKey(this.ParentModelLink, childModelLink);
        }
	}

	internal sealed class ForeignKeyRelationshipDemand : IRelationshipDemand
	{
        internal DataTable ParentTable { get; private set; }

        internal Attribution.ForeignKey ForeignKey { get; private set; }

        internal ForeignKeyRelationshipDemand(DataTable parentTable, Attribution.ForeignKey foreignKey)
		{
            this.ParentTable = parentTable;
            this.ForeignKey = foreignKey;
		}

        public void CreateForeignKey(DataSet dataSet)
		{
            this.ForeignKey.CreateForeignKey(dataSet, this.ParentTable);
		}
	}
}
