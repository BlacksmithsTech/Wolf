using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class ModelDefinition
    {
        private bool _isCollection;
        private string[] _sources;
        private ModelLink _target;
        private ModelLink _source;
        private Utility.MemberAccessor _memberAccessor;

        internal string Name { get; private set; }
        internal Type MemberType { get; private set; }
        internal Type CollectionType { get; private set; }
        internal TypeDefinition TypeDefinition { get; private set; }
        internal ModelDefinition ParentModel { get; private set; }
        internal ModelDefinition[] NestedModels { get; private set; }

        internal ModelDefinition(MemberInfo member, TypeDefinitionCollection typeLinks, ModelDefinition parentModel = null)
        {
            this._memberAccessor = Utility.MemberAccessor.Create(member);
            this.Name = member.Name;
            this.MemberType = Utility.ReflectionHelper.GetMemberType(member);
            this.CollectionType = Utility.ReflectionHelper.GetCollectionType(member);
            this._isCollection = null != this.CollectionType;
            if (!this._isCollection)
                this.CollectionType = this.MemberType;
            this.TypeDefinition = typeLinks.GetOrCreate(this.CollectionType);

            this.ParentModel = parentModel;
            this.NestedModels = this.TypeDefinition.ComplexMembers.Select(mi => new ModelDefinition(mi, typeLinks, this)).ToArray();
        }

        internal void Flatten(DataSet ds, object source, Dictionary<Type, FlattenedCollection> Collections)
        {
            var thisCollection = this.ToCollection(source);
            if (Collections.ContainsKey(this.CollectionType))
            {
                Collections[this.CollectionType].AddCollection(this, source, thisCollection);
            }
            else
            {
                Collections.Add(this.CollectionType, new FlattenedCollection(ds, this, source, thisCollection));
            }

            foreach (var nm in this.NestedModels)
                foreach (var no in thisCollection)
                    nm.Flatten(ds, no, Collections);
        }

        internal IEnumerable<T> GetAttributes<T>() where T : Attribute
        {
            return this._memberAccessor.Member.GetCustomAttributes<T>().Union(this.CollectionType.GetCustomAttributes<T>());
        }

        internal ModelLink GetModelTarget(DataSet ds)
        {
            if(null == this._target)
            {
                this._target =
                    this.GetModelSource(ds)
                    ?? new ModelLink(this, this.AutoGenerateTable(ds)); //No target or source - automatically generate a table for the model
            }
            return this._target;
        }

        internal ModelLink GetModelSource(DataSet ds)
        {
            if (null == this._source)
            {
                DataTable sourceDt = null;
                foreach (var source in this.GetSources())
                {
                    sourceDt = Utility.DataTableHelpers.GetByNormalisedName(ds, source);
                    if (null != sourceDt)
                        break;
                }

                if (null != sourceDt)
                    this._source = new ModelLink(this, sourceDt);
            }
            return this._source;
        }

        internal bool MemberEquals(MemberInfo mi)
        {
            return this._memberAccessor.Member.Equals(mi);
        }

        internal System.Collections.IList ToCollection(object source)
        {
            if (this._isCollection)
                return (System.Collections.IList)this.GetValue(source);
            else
            {
                var Value = this.GetValue(source);
                return null != Value ? new[] { Value } : new object[0];
            }
        }

        private object GetValue(object source)
        {
            return this._memberAccessor.GetValue(source);
        }

        internal void SetValue(object source, IEnumerable<object> value)
        {
            if (this.MemberType.IsArray)
                this.SetValue(source, Utility.ReflectionHelper.ArrayFromList(this.CollectionType, value.ToArray()));
            else
                this.SetValue(source, (System.Collections.IList)value.ToArray());
        }

        internal void SetValue(object source, System.Collections.IList value)
        {
            if (this._isCollection)
                this._memberAccessor.SetValue(source, value);
            else
                this._memberAccessor.SetValue(source, value.Cast<object>().FirstOrDefault());
        }

        //internal IEnumerable<object> ToEnumerable(object source)
        //{
        //	return this.ToCollection(source).Cast<object>().Where(o => null != o);
        //}

        internal string[] GetSources()
        {
            if (null == this._sources)
            {
                // Asc order sensitive.
                var Ret = this._memberAccessor.Member.GetCustomAttributes<Attribution.Source>()
                    .Concat(this.CollectionType.GetCustomAttributes<Attribution.Source>())
                    .Select(a => a.From)
                    .ToList();

                // When no sources have been defined programatically or via decoration, the type name is used
                if (0 == Ret.Count)
                {
                    if (ModelDefinition.CheckIfAnonymousType(this.CollectionType))
                        throw new ArgumentException($"The type '{this.CollectionType}' couldn't participate in the database model because it is anonymous and defines no sources.");
                    Ret.Add(this.CollectionType.Name);
                }

                // Normalise the source names into fully qualified SQL names
                this._sources = Ret.Select(s => Utility.StringHelpers.GetQualifiedSqlName(s).ToString()).ToArray();
            }
            return this._sources;
        }

        private static bool CheckIfAnonymousType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // HACK: The only way to detect anonymous types right now.
            return Attribute.IsDefined(type, typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false)
                && type.Name.Contains("AnonymousType")
                && (type.Name.StartsWith("<>") || type.Name.StartsWith("VB$"))
                && type.Attributes.HasFlag(TypeAttributes.NotPublic);
        }

        private string GetDefaultTableNameForType()
        {
            // ** Try to obtain a default source
            var Sources = this.GetSources();

            if (1 == Sources.Length)
                return Sources[0]; // Single source, assume this is the table name

            //TODO: Default via further attribution?

            if (Sources.Length > 1)
                throw new InvalidOperationException($"The model member '{this.Name}' ({this.MemberType}) specifies multiple sources. A target table to commit changes into can't be determined.");
            else
                throw new InvalidOperationException($"A target table for the model member '{this.Name}' ({this.MemberType}) couldn't be determined.");
        }

        private DataTable AutoGenerateTable(DataSet ds)
        {
            var tn = Utility.StringHelpers.GetQualifiedSqlName(this.GetDefaultTableNameForType());
            var dt = new DataTable(tn.Name, tn.Schema);

            foreach (var member in this.TypeDefinition.PrimitiveMembers)
                if (!dt.Columns.Contains(member.Name))
                    dt.Columns.Add(member.Name, Utility.ReflectionHelper.GetMemberType(member));

            ds.Tables.Add(dt);
            return dt;
        }
    }
}
