using System;
using System.Collections.Generic;
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
        internal Type MemberElementType;//gets the underlying element type from nullables, lists etc. i.e. the "T" from MemberType<T>

        private string[] _enumNames;
        private Lazy<string> _fallbackEnum;

        internal MemberLink(MemberInfo m, DataColumn c)
        {
            this.Member = Utility.MemberAccessor.Create(m);
            this.MemberType = Utility.ReflectionHelper.GetMemberType(m);
            this.MemberElementType = Utility.ReflectionHelper.GetMemberTypeOrGenericType(m);
            this.Column = c;
        }

        internal bool IsKey(Attribution.Key.KeyType keyType)
		{
            return this.Member.Member.GetCustomAttributes<Attribution.Key>().Any(ka => keyType.Equals(ka.Type));
		}

        internal IEnumerable<Attribution.ForeignKey> GetForeignKeys()
		{
            return this.Member.Member.GetCustomAttributes<Attribution.ForeignKey>();
		}

        internal object GetValue(object source)
        {
            return this.Member.GetValue(source);
        }

        internal void SetValue(object source, object value)
        {
			if (value is string && this.MemberElementType.IsEnum)
			{
                if (null == this._enumNames)
                    this._enumNames = Enum.GetNames(this.MemberElementType);
                var enumValue = this._enumNames.FirstOrDefault(ev => ev.Equals((string)value, StringComparison.CurrentCultureIgnoreCase));

                if (null != enumValue)
                {
                    value = Enum.Parse(this.MemberElementType, enumValue);
                }
                else
                {
                    // ** Attempt a fallback enum
                    if(null == this._fallbackEnum)
                        this._fallbackEnum = new Lazy<string>(() =>
                        {
                            return this._enumNames.FirstOrDefault(ev => this.MemberElementType.GetField(ev).GetCustomAttributes(typeof(Attribution.FallbackEnum), false).Any());
                        });

                    if (null != this._fallbackEnum.Value)
                        value = Enum.Parse(this.MemberElementType, this._fallbackEnum.Value);
                    else
                        throw new ArgumentException($"Database value '{value}' not found within enumeration of type '{this.MemberElementType}' at '{Utility.StringHelpers.GetFullMemberName(this.Member)}'");
                }
            }

			this.Member.SetValue(source, value);
        }
    }

    
}
