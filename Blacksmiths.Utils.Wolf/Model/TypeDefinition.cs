﻿using System;
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
        //internal delegate object ObjectFinder(ModelLink modelLink, DataRow r, IEnumerable<object> collection);

        internal MemberInfo[] Members { get; private set; }
        //internal ObjectFinder FindObject { get; private set; }
        internal Type Type { get; private set; }

        internal IEnumerable<MemberInfo> PrimitiveMembers
        {
            get
            {
                return this.Members
                    .Where(m => Utility.ReflectionHelper.IsPrimitive(Utility.ReflectionHelper.GetMemberType(m)));
            }
        }

        internal IEnumerable<MemberInfo> ComplexMembers
        {
            get
            {
                return this.Members
                    .Where(m => !Utility.ReflectionHelper.IsPrimitive(Utility.ReflectionHelper.GetMemberType(m)));
            }
        }

        internal TypeDefinition(Type t, TypeDefinitionCollection typeDefinitions)
        {
            this.Type = t;

            this.Members = this.Type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                .Where(m =>
                {
                    if (m.MemberType == MemberTypes.Property)
                        return ((PropertyInfo)m).CanWrite; //only writable properties are considered
                    else if (m.MemberType == MemberTypes.Field)
                        return true;
                    else
                        return false;
                })
                .ToArray();
        }

        internal Dictionary<string, MemberLink> GetLinkedMembersFor(DataTable dt)
        {
            return this.PrimitiveMembers
                .Select(m => new MemberLink(m, dt.Columns[m.Name]))
                .ToDictionary(k => k.Member.Name);
        }

        internal object FindObject(DataRow r, IEnumerable<object> collection, IEnumerable<MemberLink> keyColumns)
        {
            foreach (var o in collection)
            {
                bool Equal = true;

                foreach (var ml in keyColumns)//keyColumns can be the PK or it could be all columns if there's no PK
                {
                    object value = ml.GetValue(o) ?? DBNull.Value;

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
