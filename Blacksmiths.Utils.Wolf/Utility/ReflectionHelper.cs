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
using System.Linq.Expressions;

namespace Blacksmiths.Utils.Wolf.Utility
{
	public static class ReflectionHelper
	{
		public static Array ArrayFromList(Type CollectionType, System.Collections.IList Collection)
		{
			var a = Array.CreateInstance(CollectionType, Collection.Count);
            Collection.CopyTo(a, 0);
            return a;
		}

        public static System.Collections.IList ListFromList(Type collectionType, IEnumerable<object> collection = null)
        {
            var t = typeof(List<>).MakeGenericType(collectionType);
            var l = (System.Collections.IList)Activator.CreateInstance(t);
            if (null != collection)
                foreach (var value in collection)
                    l.Add(value);
            return l;
        }

        public static object GetValue(MemberInfo Member, object source)
		{
			if (Member is FieldInfo fi)
				return fi.GetValue(source);
			else if (Member is PropertyInfo pi)
				return pi.GetValue(source, null);
			else
				return null;
		}

		public static void SetValue(MemberInfo Member, object source, object value)
		{
            if (Member is FieldInfo fi)
            {
                fi.SetValue(source, value);
            }
            else if (Member is PropertyInfo pi)
            {
                if (pi.CanWrite)
                    pi.SetValue(source, value, null);
                else
                    throw new FieldAccessException($"Tried writing to {GetMemberDisplayName(Member)} but it was read only");
            }
		}

        public static string GetMemberDisplayName(MemberInfo Member)
        {
            return $"{Member.DeclaringType.Name}.{Member.Name}";
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

        public static Type GetMemberTypeOrGenericType(MemberInfo Member)
        {
            var t = GetMemberType(Member);
            if (t.IsGenericType)
                return t.GetGenericArguments()[0];
            else
                return t;
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
			var assignable = x.IsAssignableFrom(y);
            if(!assignable)
			{
                // ** .NET can't automatically make the assignment. Can Wolf assist?
                assignable |= typeof(string) == x && y.IsEnum;
			}
            return assignable;
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
                typeof(Enum),
			};

			return PrimitiveTypes.Any(t => IsAssignable(t, x));
		}
	}

    internal class MemberAccessor
    {
        internal MemberInfo Member;

        internal string Name { get { return this.Member.Name; } }

        protected MemberAccessor(MemberInfo m)
        {
            this.Member = m;
        }

        internal static MemberAccessor Create(MemberInfo m)
        {
            if (m is PropertyInfo pi)
                return new PropertyAccessor(pi);
            else
                return new MemberAccessor(m);
        }

        internal virtual object GetValue(object source)
        {
            return Utility.ReflectionHelper.GetValue(this.Member, source);
        }

        internal virtual void SetValue(object source, object value)
        {
            Utility.ReflectionHelper.SetValue(this.Member, source, value);
        }
    }

    internal sealed class PropertyAccessor : MemberAccessor
    {
        private Func<object, object> Getter;
        private Action<object, object> Setter;

        internal PropertyInfo Property { get { return (PropertyInfo)this.Member; } }

        internal PropertyAccessor(PropertyInfo pi)
            : base(pi)
        {
            this.CompileGetter();
            this.CompileSetter();
        }

        private void CompileGetter()
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            UnaryExpression instanceCast = (!this.Property.DeclaringType.IsValueType) ? Expression.TypeAs(instance, this.Property.DeclaringType) : Expression.Convert(instance, this.Property.DeclaringType);
            this.Getter = Expression.Lambda<Func<object, object>>(Expression.TypeAs(Expression.Call(instanceCast, this.Property.GetGetMethod()), typeof(object)), instance).Compile();
        }

        private void CompileSetter()
        {
            var instance = Expression.Parameter(typeof(object), "instance");
            var value = Expression.Parameter(typeof(object), "value");
            UnaryExpression instanceCast = (!this.Property.DeclaringType.IsValueType) ? Expression.TypeAs(instance, this.Property.DeclaringType) : Expression.Convert(instance, this.Property.DeclaringType);
            UnaryExpression valueCast = (!this.Property.PropertyType.IsValueType) ? Expression.TypeAs(value, this.Property.PropertyType) : Expression.Convert(value, this.Property.PropertyType);
            this.Setter = Expression.Lambda<Action<object, object>>(Expression.Call(instanceCast, this.Property.GetSetMethod(), valueCast), new ParameterExpression[] { instance, value }).Compile();
        }

        internal override object GetValue(object source)
        {
            return this.Getter(source);
        }

        internal override void SetValue(object source, object value)
        {
            this.Setter(source, value);
        }
    }
}
