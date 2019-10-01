using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class MemberRelationshipDemand
    {
        private System.Collections.IList parentCollection;

        internal ModelLink ParentModelLink { get; private set; }
        internal ModelDefinition ChildModelDefinition { get; private set; }

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
            else
            {
                // ** No relationship. Cross join all children with a reference to the same collection
                //var Values = this.Relationship.MemberType.IsArray ? Utility.ReflectionHelper.ArrayFromList(this.Relationship.CollectionType, sourceCollection) : sourceCollection;
                foreach (var parenti in this.parentCollection)
                    childLink.ModelDefinition.SetValue(parenti, sourceCollection);
            }
        }
    }
}
