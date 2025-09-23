using Blacksmiths.Utils.Wolf.Attribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Blacksmiths.Utils.Wolf.Utility
{
    internal class TypeCacheCollection
    {
        private readonly Dictionary<Type, List<object>> rowCollection;
        private readonly Dictionary<Type, TypeCache> typeCollection = new Dictionary<Type, TypeCache>();
        internal TypeCacheCollection(Dictionary<Type, List<object>> rowCollection)
        {
            this.rowCollection = rowCollection;
        }

        internal TypeCache Get(Type type)
        {
            if (!typeCollection.ContainsKey(type))
                typeCollection.Add(type, new TypeCache(type, this.rowCollection));
            return typeCollection[type];
        }
    }

    internal class TypeCache
    {
        public Type Type { get; set; }
        public List<MemberCache> Members { get; set; } = new List<MemberCache>();

        public TypeCache(Type type, Dictionary<Type, List<object>> rowCollection)
        {
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance);

            foreach (var member in members)
            {
                var property = member as PropertyInfo;
                var field = member as FieldInfo;

                if (null == property && null == field) continue;
                if (null != property && !property.CanWrite) continue;

                var memberType = property?.PropertyType ?? field.FieldType;

                // Check if property type is in rowCollection keys
                var targetType = GraphMapper.GetTargetType(memberType);
                if (null != targetType && rowCollection.ContainsKey(targetType))
                    this.Members.Add(new MemberCache(member, memberType, targetType, rowCollection[targetType]));
            }
        }
    }

    internal class MemberCache
    {
        public MemberInfo Member { get;}
        public Type MemberType { get; }
        public Type TargetType { get; }
        public List<object> Data { get; }
        public MemberAccessor Accessor { get; }
        public RelationCache Relation { get; }
        public MemberCache(MemberInfo memberInfo, Type memberType, Type targetType, List<object> data)
        {
            this.Member = memberInfo;
            this.Accessor = MemberAccessor.Create(memberInfo);
            this.MemberType = memberType;
            this.TargetType = targetType;
            this.Relation = RelationCache.Create(this.Member.GetCustomAttribute<Relation>(), this.Member.DeclaringType, targetType);
            this.Data = data;
        }

        public bool IsMapped(object obj) => null != this.Accessor.GetValue(obj);
    }

    internal class RelationCache
    {
        public Relation Relation { get; }
        public Type ParentType { get; }
        public Type ChildType { get; }
        private readonly MemberAccessor[] parentAccessors;
        private readonly MemberAccessor[] childAccessors;

        public RelationCache(Relation relation, Type parentType, Type childType)
        {
            if (!relation.IsValid())
                throw new InvalidOperationException("Relation attribute has invalid field name configuration");

            this.Relation = relation;
            this.ParentType = parentType;
            this.ChildType = childType;

            this.parentAccessors = new MemberAccessor[this.Relation.ParentFieldNames.Length];
            this.childAccessors = new MemberAccessor[this.Relation.ChildFieldNames.Length];

            for (int i = 0; i < this.Relation.ParentFieldNames.Length; i++)
            {
                var parentFieldName = this.Relation.ParentFieldNames[i];
                var childFieldName = this.Relation.ChildFieldNames[i];

                var parentProperty = MemberAccessor.Create(parentType.GetProperty(parentFieldName));
                var childProperty = MemberAccessor.Create(childType.GetProperty(childFieldName));

                if (parentProperty == null || childProperty == null)
                    throw new InvalidOperationException($"Field name '{parentFieldName}' or '{childFieldName}' not found on respective types");

                this.parentAccessors[i] = parentProperty;
                this.childAccessors[i] = childProperty;
            }
        }

        public static RelationCache Create(Relation relation, Type parentType, Type childType)
        {
            if (null != relation)
                return new RelationCache(relation, parentType, childType);
            else
                return null;
        }

        public List<object> Filter(object parent, List<object> data)
        {
            var filteredData = new List<object>();

            foreach (var child in data)
            {
                bool matches = true;
                for (int i = 0; i < this.parentAccessors.Length; i++)
                {
                    var parentValue = this.parentAccessors[i].GetValue(parent);
                    var childValue = this.childAccessors[i].GetValue(child);

                    if (!Equals(parentValue, childValue))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    filteredData.Add(child);
                }
            }

            return filteredData;
        }
    }

    /// <summary>
    /// Provides mapping functionality for converting row collections to graph objects
    /// </summary>
    internal static class GraphMapper
    {
        ///// <summary>
        ///// Maps a dictionary of row collections into the provided graph instance
        ///// </summary>
        //public static TGraph MapList<TGraph>(TGraph instance, Dictionary<Type, List<object>> rowCollection)
        //{
        //    if (null != instance)
        //        MapProperties(instance, rowCollection);
        //    return instance;
        //}

        /// <summary>
        /// Maps a dictionary of row collections to a list of graph objects
        /// </summary>
        /// <typeparam name="TGraph">The target graph type</typeparam>
        /// <param name="rowCollection">Dictionary mapping types to their row data</param>
        /// <returns>List of mapped graph objects</returns>
        public static List<TGraph> MapList<TGraph>(Dictionary<Type, List<object>> rowCollection)
        {
            var result = new List<TGraph>();
            var typeCache = new TypeCacheCollection(rowCollection);

            if (!rowCollection.ContainsKey(typeof(TGraph)))
            {
                // If TGraph is not a key, return a single new instance
                var newInstance = CreateAndMapInstance<TGraph>(typeCache);
                if (newInstance != null)
                {
                    result.Add(newInstance);
                }
            }
            else
            {
                // If TGraph is a key, map all items from the collection
                var sourceItems = rowCollection[typeof(TGraph)];
                foreach (var item in sourceItems)
                {
                    if (item is TGraph graphItem)
                    {
                        MapProperties(graphItem, typeCache);
                        result.Add(graphItem);
                    }
                }
            }

            return result;
        }

        private static TGraph CreateAndMapInstance<TGraph>(TypeCacheCollection typeCache)
        {
            return MapProperties(Activator.CreateInstance<TGraph>(), typeCache);
        }

        
        private static object MapProperties(object instance, Type instanceType, TypeCacheCollection typeCache)
        {
            foreach (var mappableMember in typeCache.Get(instanceType).Members.Where(m => !m.IsMapped(instance)))
            {
                if (null != mappableMember.Relation)
                {
                    // Apply relation filtering
                    var filteredData = mappableMember.Relation.Filter(instance, mappableMember.Data);
                    instance = SetPropertyValue(instance, mappableMember, filteredData, typeCache);
                }
                else
                {
                    // Direct mapping without relation filtering
                    instance = SetPropertyValue(instance, mappableMember, mappableMember.Data, typeCache);
                }
            }

            return instance;
        }

        private static TGraph MapProperties<TGraph>(TGraph instance, TypeCacheCollection typeCache)
            => (TGraph)MapProperties(instance, typeof(TGraph), typeCache);


        internal static Type GetTargetType(Type propertyType)
        {
            // Handle collections
            if (propertyType.IsArray)
            {
                return propertyType.GetElementType();
            }

            if (propertyType.IsGenericType)
            {
                var genericTypeDefinition = propertyType.GetGenericTypeDefinition();
                if (genericTypeDefinition == typeof(List<>) ||
                    genericTypeDefinition == typeof(IList<>) ||
                    genericTypeDefinition == typeof(ICollection<>) ||
                    genericTypeDefinition == typeof(IEnumerable<>))
                {
                    return propertyType.GetGenericArguments()[0];
                }
            }

            // Handle non-collections
            return propertyType;
        }

        private static object SetPropertyValue<T>(object instance, MemberCache member, List<T> value, TypeCacheCollection typeCache)
        {
            if (value == null) return instance;

            var convertedValue = ConvertToTargetType<T>(value, member, typeCache);
            member.Accessor.SetValue(instance, convertedValue);
            return instance;
        }

        private static object ConvertToTargetType<T>(List<T> value, MemberCache member, TypeCacheCollection typeCache)
        {
            if (member.MemberType.IsArray)
            {
                var result = Array.CreateInstance(member.TargetType, value.Count);
                for (var i = 0; i < value.Count; i++)
                    result.SetValue(MapProperties(value[i], member.TargetType, typeCache), i);

                return result;
            }
            else if (member.MemberType is System.Collections.IList)
            {
                throw new NotImplementedException();
            }
            else
            {
                return value.SingleOrDefault();
            }
        }
    }
}
