using Blacksmiths.Utils.Wolf.Attribution;
using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmiths.Utils.Wolf
{
    public static class ColumnAttributeTypeMap
    {
        public static void SetTypeMap<T>()
        {
            SqlMapper.SetTypeMap(typeof(T), new ColumnAttributeTypeMap<T>());
        }
    }

    public class ColumnAttributeTypeMap<T> : SqlMapper.ITypeMap
    {
        private readonly SqlMapper.ITypeMap _defaultMapper;
        private readonly SqlMapper.ITypeMap _customMapper;

        public ColumnAttributeTypeMap()
        {
            _defaultMapper = new DefaultTypeMap(typeof(T));

            _customMapper = new CustomPropertyTypeMap(
                typeof(T),
                (type, columnName) =>
                    type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(prop =>
                            prop.GetCustomAttributes(typeof(ColumnAttribute), false)
                                .Cast<ColumnAttribute>()
                                .Any(attr => string.Equals(attr.Name, columnName, StringComparison.OrdinalIgnoreCase)))
            );
        }

        public ConstructorInfo FindConstructor(string[] names, Type[] types) =>
            _defaultMapper.FindConstructor(names, types);

        public ConstructorInfo FindExplicitConstructor() =>
            _defaultMapper.FindExplicitConstructor();

        public SqlMapper.IMemberMap GetConstructorParameter(ConstructorInfo constructor, string columnName) =>
            _defaultMapper.GetConstructorParameter(constructor, columnName);

        public SqlMapper.IMemberMap GetMember(string columnName) =>
            _customMapper.GetMember(columnName) ?? _defaultMapper.GetMember(columnName);
    }
}
