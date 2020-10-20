using System;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class MemberLink
    {
        internal Utility.MemberAccessor Member;
        internal DataColumn Column;
        internal Type MemberType;

        private string[] _enumNames;

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
			if (value is string && this.MemberType.IsEnum)
			{
                if (null == this._enumNames)
                    this._enumNames = Enum.GetNames(this.MemberType);
                var enumValue = this._enumNames.FirstOrDefault(ev => ev.Equals((string)value, StringComparison.CurrentCultureIgnoreCase));
                if (null != enumValue)
                    value = Enum.Parse(this.MemberType, enumValue);
                else
                    throw new ArgumentException($"Database value '{value}' not found within enumeration of type '{this.MemberType}' at '{Utility.StringHelpers.GetFullMemberName(this.Member)}'");
            }

			this.Member.SetValue(source, value);
        }
    }

    
}
