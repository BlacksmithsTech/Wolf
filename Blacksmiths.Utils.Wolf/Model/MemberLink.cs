using System;
using System.Data;
using System.Reflection;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class MemberLink
    {
        internal Utility.MemberAccessor Member;
        internal DataColumn Column;
        internal Type MemberType;

        internal MemberLink(MemberInfo m, DataColumn c)
        {
            this.Member = Utility.MemberAccessor.Create(m);
            this.MemberType = Utility.ReflectionHelper.GetMemberType(m);
            this.Column = c;
        }

        internal object GetValue(object source)
        {
            return this.Member.GetValue(source);
        }

        internal void SetValue(object source, object value)
        {
            this.Member.SetValue(source, value);
        }
    }

    
}
