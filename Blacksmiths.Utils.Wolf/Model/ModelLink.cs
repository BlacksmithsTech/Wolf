using System;
using System.Collections.Generic;
using System.Text;
using System.Data;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class ModelLink
    {
        private Dictionary<string, MemberLink> memberDict;
        private DataView _view;

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

        internal DataView GetDataView()
        {
            if (null == this._view)
            {
                this._view = new DataView(this.Data);

                if (null != this.ModelDefinition.ParentModel)
                {

                }
            }

            return this._view;
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
    }
}
