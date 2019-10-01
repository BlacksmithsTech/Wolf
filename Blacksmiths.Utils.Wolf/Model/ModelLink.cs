using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class ModelLink
    {
        private Dictionary<string, MemberLink> memberDict;

        internal MemberLink this[string Name]
        {
            get
            {
                return this.memberDict[Name];
            }
        }

        internal IEnumerable<MemberLink> Members { get { return this.memberDict.Values; } }
        internal DataTable Data { get; private set; }
        internal ModelDefinition ModelDefinition { get; private set; }

        internal ModelLink(ModelDefinition md, DataTable dt)
        {
            this.ModelDefinition = md;
            this.Data = dt;
            this.memberDict = this.ModelDefinition.TypeDefinition.GetLinkedMembersFor(this.Data);
        }

        internal DataView GetDataView(ModelLink parentModelLink, object parent)
        {
            Utility.PerfDebuggers.BeginTrace("Obtaining DataView");

            var Ret = new DataView(this.Data);

            if (null != this.ModelDefinition.ParentModel)
            {
                // ** Apply relationship
                var Relationship = this.FindFirstValidRelationshipWithParent(parentModelLink);

                var sb = new StringBuilder();
                foreach (var ChildKeyName in Relationship.ChildFieldNames)
                {
                    if (sb.Length > 0)
                        sb.Append(" AND ");
                    sb.Append($"{ChildKeyName} = '{parentModelLink[ChildKeyName].GetValue(parent)}'");//TODO: Encode
                }
                Ret.RowFilter = sb.ToString();
            }

            Utility.PerfDebuggers.EndTrace("Obtaining DataView");

            return Ret;
        }

        internal bool ContainsMember(string Name)
        {
            return this.memberDict.ContainsKey(Name);
        }

        internal IComparable GetKey(object source, string[] KeyNames)
        {
            switch (KeyNames.Length)
            {
                case 1:
                    return new Tuple<object>(this[KeyNames[0]].GetValue(source));
                default:
                    throw new InvalidOperationException("Unsupported key length");
            }
        }

        internal Attribution.Relation FindFirstValidRelationshipWithParent(ModelLink parentLink)
        {
            foreach (var Relation in this.ModelDefinition.GetAttributes<Attribution.Relation>())
            {
                if (Relation.IsSane()
                    && Relation.ParentFieldNames.All(fn => parentLink.ContainsMember(fn))
                    && Relation.ChildFieldNames.All(fn => this.ContainsMember(fn)))
                {
                    return Relation;
                }
            }
            return null;
        }
    }
}
