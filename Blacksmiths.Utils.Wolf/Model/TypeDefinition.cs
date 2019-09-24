using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class TypeDefinition
    {
        internal delegate DataRow RowFinder(object o);
        internal delegate object ObjectFinder(ModelLink modelLink, DataRow r, IEnumerable<object> collection);

        internal MemberInfo[] Members { get; private set; }
        internal ModelDefinition[] NestedModels { get; private set; }
        internal ObjectFinder FindObject { get; private set; }
        internal Type Type { get; private set; }

        internal IEnumerable<MemberInfo> PrimitiveMembers
        {
            get
            {
                return this.Members
                    .Where(m => Utility.ReflectionHelper.IsPrimitive(Utility.ReflectionHelper.GetMemberType(m)));
            }
        }

        internal TypeDefinition(Type t, TypeDefinitionCollection typeDefinitions)
        {
            this.Type = t;

            this.Members = this.Type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => new[] { MemberTypes.Property, MemberTypes.Field }.Contains(m.MemberType))
                .ToArray();

            this.NestedModels = this.Members
                .Where(m => !Utility.ReflectionHelper.IsPrimitive(Utility.ReflectionHelper.GetMemberType(m)))
                .Select(m => new ModelDefinition(m, typeDefinitions))
                .ToArray();

            this.FindObject = this.FindObject_FullEquality;
        }

        internal Dictionary<string, MemberLink> GetLinkedMembersFor(DataTable dt)
        {
            return this.PrimitiveMembers
                .Select(m => new MemberLink(m, dt.Columns[m.Name]))
                .ToDictionary(k => k.Member.Name);
        }

        internal object FindObject_FullEquality(ModelLink modelLink, DataRow r, IEnumerable<object> collection)
        {
            foreach (var o in collection)
            {
                bool Equal = true;

                foreach (var ml in modelLink.Members)
                {
                    object value = ml.GetValue(o);

                    if (!r[ml.Column].Equals(value))
                    {
                        Equal = false;
                        break;
                    }
                }

                if (Equal)
                    return o;
            }

            return null;
        }
    }
}
