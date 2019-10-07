using System;
#if NET40
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Blacksmiths.Utils.Wolf
{
    internal static class Net40Shims
    {

        internal static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo member)
        {
            return member.GetCustomAttributes(typeof(T), true).Cast<T>();
        }
    }
}
#endif