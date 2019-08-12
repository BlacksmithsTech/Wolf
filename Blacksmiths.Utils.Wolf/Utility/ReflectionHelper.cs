using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Utility
{
	public static class ReflectionHelper
	{
		public static object GetValue(MemberInfo Member, object source)
		{
			if (Member is FieldInfo fi)
				return fi.GetValue(source);
			else if (Member is PropertyInfo pi)
				return pi.GetValue(source);
			else
				return null;
		}

		public static void SetValue(MemberInfo Member, object source, object value)
		{
			if (Member is FieldInfo fi)
				fi.SetValue(source, value);
			else if (Member is PropertyInfo pi)
				pi.SetValue(source, value);
		}

		public static Type GetMemberType(MemberInfo Member)
		{
			if (Member is FieldInfo fi)
				return fi.FieldType;
			else if (Member is PropertyInfo pi)
				return pi.PropertyType;
			else
				return null;
		}

		public static Type GetCollectionType(MemberInfo Member)
		{
			var mt = GetMemberType(Member);
			if (mt.IsArray)
				return mt.GetElementType();
			else
				return mt.GetInterfaces().FirstOrDefault(i => i.IsGenericType && typeof(ICollection<>).Equals(i.GetGenericTypeDefinition()))?.GetGenericArguments()[0];
		}
	}
}
