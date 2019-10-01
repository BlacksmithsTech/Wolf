using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class ModelLink
    {
        private Dictionary<string, MemberLink> _members;
        private List<MemberLink> _keyColumns;

        internal MemberLink this[string Name]
        {
            get
            {
                return this._members[Name];
            }
        }

        internal IEnumerable<MemberLink> Members { get { return this._members.Values; } }
        internal DataTable Data { get; private set; }
        internal ModelDefinition ModelDefinition { get; private set; }

        /// <summary>
        /// Gets the PK columns if they are known, or otherwise returns all members as the key
        /// </summary>
        internal IEnumerable<MemberLink> KeyColumns { get { return this._keyColumns.Count > 0 ? this._keyColumns : this.Members; } }
        internal ModelLink(ModelDefinition md, DataTable dt)
        {
            this.ModelDefinition = md;
            this.Data = dt;
            this._members = this.ModelDefinition.TypeDefinition.GetLinkedMembersFor(this.Data);
            this.IdentifyKeyColumns();
        }

        private void IdentifyKeyColumns()
        {
            // ** Identify the key columns
            this._keyColumns = new List<MemberLink>(this._members.Values.Where(m => this.Data.PrimaryKey.Any(pkc => pkc == m.Column)));//Attribution based PKs?
        }

        internal void ThrowIfCantUpdate()
        {
            if (this.Data.Rows.Count > 0)
            {
                if (0 == this._keyColumns.Count) //Re-identify. Key information is loaded late for perf reasons (may not need PK info if the user is only doing a read)
                    this.IdentifyKeyColumns();
                if (0 == this._keyColumns.Count) //This will prevent commits to keyless tables for now, but will see if anyone needs this before allowing users to accidentally express their UPDATEs as DELETE+INSERT.
                    throw new InvalidOperationException("Your model contains existing data which potentially to UPDATE, but there is no primary key defined.");
            }
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
            return this._members.ContainsKey(Name);
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
