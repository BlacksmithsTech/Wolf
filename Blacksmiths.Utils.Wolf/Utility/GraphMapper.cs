using Blacksmiths.Utils.Wolf.Attribution;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmiths.Utils.Wolf.Utility
{
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

            if (!rowCollection.ContainsKey(typeof(TGraph)))
            {
                // If TGraph is not a key, return a single new instance
                var newInstance = CreateAndMapInstance<TGraph>(rowCollection);
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
                        MapProperties(graphItem, rowCollection);
                        result.Add(graphItem);
                    }
                }
            }

            return result;
        }

        private static TGraph CreateAndMapInstance<TGraph>(Dictionary<Type, List<object>> rowCollection)
        {
            return MapProperties(Activator.CreateInstance<TGraph>(), rowCollection);
        }

        private static TGraph MapProperties<TGraph>(TGraph instance, Dictionary<Type, List<object>> rowCollection)
        {
            var members = typeof(TGraph).GetMembers(BindingFlags.Public | BindingFlags.Instance);

            foreach (var member in members)
            {
                var property = member as PropertyInfo;
                var field = member as FieldInfo;

                if (null == property && null == field) continue;
                if (null != property && !property.CanWrite) continue;
                
                var relationAttribute = member.GetCustomAttribute<Relation>();
                var memberType = property?.PropertyType ?? field.FieldType;

                // Check if property type is in rowCollection keys
                var targetType = GetTargetType(memberType);
                if (targetType != null && rowCollection.ContainsKey(targetType))
                {
                    var sourceData = rowCollection[targetType];

                    if (relationAttribute != null)
                    {
                        // Apply relation filtering
                        var filteredData = ApplyRelationFilter(instance, sourceData, relationAttribute, memberType);
                        instance = (TGraph)SetPropertyValue(instance, member, filteredData, memberType);
                    }
                    else
                    {
                        // Direct mapping without relation filtering
                        instance = (TGraph)SetPropertyValue(instance, member, sourceData, memberType);
                    }
                }
            }

            return instance;
        }

        private static Type GetTargetType(Type propertyType)
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

        private static List<object> ApplyRelationFilter<TGraph>(TGraph parentInstance, List<object> childData, Relation relation, Type targetPropertyType)
        {
            if (!relation.IsValid())
            {
                throw new InvalidOperationException("Relation attribute has invalid field name configuration");
            }

            var parentType = typeof(TGraph);
            var childType = GetTargetType(targetPropertyType);
            if (childType == null) return new List<object>();

            var filteredData = new List<object>();

            foreach (var childItem in childData)
            {
                bool matches = true;
                for (int i = 0; i < relation.ParentFieldNames.Length; i++)
                {
                    var parentFieldName = relation.ParentFieldNames[i];
                    var childFieldName = relation.ChildFieldNames[i];

                    var parentProperty = MemberAccessor.Create(parentType.GetProperty(parentFieldName));
                    var childProperty = MemberAccessor.Create(childType.GetProperty(childFieldName));

                    if (parentProperty == null || childProperty == null)
                    {
                        throw new InvalidOperationException($"Field name '{parentFieldName}' or '{childFieldName}' not found on respective types");
                    }

                    var parentValue = parentProperty.GetValue(parentInstance);
                    var childValue = childProperty.GetValue(childItem);

                    if (!Equals(parentValue, childValue))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    filteredData.Add(childItem);
                }
            }

            return filteredData;
        }

        private static object SetPropertyValue<T>(object instance, MemberInfo member, List<T> value, Type targetPropertyType)
        {
            if (value == null) return instance;

            var convertedValue = ConvertToTargetType<T>(value, targetPropertyType);
            if (member is PropertyInfo property)
                property.SetValue(instance, convertedValue);
            else if (member is FieldInfo field)
                field.SetValue(instance, convertedValue);

            return instance;
        }

        private static object ConvertToTargetType<T>(List<T> value, Type targetPropertyType)
        {
            if (targetPropertyType.IsArray)
            {
                var result = Array.CreateInstance(GetTargetType(targetPropertyType), value.Count);
                for (var i = 0; i < value.Count; i++)
                    result.SetValue(value[i], i);

                return result;
            }
            else if (targetPropertyType is System.Collections.IList)
            {
                return value;
            }
            else
            {
                return value.SingleOrDefault();
            }
        }
    }
}
