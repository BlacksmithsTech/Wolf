/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Utility
{
	public static class ReflectionHelper
	{
		public static Array ArrayFromList(Type CollectionType, System.Collections.IList Collection)
		{
			var a = Array.CreateInstance(CollectionType, Collection.Count);
			Array.Copy(Collection.Cast<object>().ToArray(), a, Collection.Count);
			return a;
		}

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

		public static bool IsAssignable(Type x, Type y)
		{
			y = Nullable.GetUnderlyingType(y) ?? y;
			return x.IsAssignableFrom(y);
		}

		public static bool IsPrimitive(Type x)
		{
			// ** Wolf defines primitive types as those that should be data bound directly to a column
			var PrimitiveTypes = new[]
			{
				typeof(bool),
				typeof(byte),
				typeof(sbyte),
				typeof(short),
				typeof(ushort),
				typeof(int),
				typeof(uint),
				typeof(long),
				typeof(ulong),
				typeof(IntPtr),
				typeof(UIntPtr),
				typeof(char),
				typeof(double),
				typeof(float),
				typeof(string),
				typeof(DateTime),
				typeof(Guid),
				typeof(decimal),
				typeof(TimeSpan),
				typeof(DateTimeOffset),
			};

			return PrimitiveTypes.Any(t => IsAssignable(t, x));
		}
	}
}
