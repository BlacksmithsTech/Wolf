﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Data;
using System.Linq;
using Blacksmiths.Utils.Wolf;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class ModelDefinition
    {
        private bool _isCollection;
        private string[] _sources;
        private string[] _targets;
        private ModelLink _target;
        private ModelLink _source;
        private Utility.MemberAccessor _memberAccessor;

        internal string Name { get; private set; }
        internal Type MemberType { get; private set; }
        internal Type CollectionType { get; private set; }
        internal TypeDefinition TypeDefinition { get; private set; }
        //internal ModelDefinition ParentModel { get; private set; }
        //internal ModelDefinition[] NestedModels { get; private set; }

        internal Lazy<IEnumerable<Attribution.Relation>> RelationshipAttributes { get; }
        private Lazy<IEnumerable<Attribution.Ignore>> IgnoreAttributes { get; }
        private Lazy<IEnumerable<Attribution.Target>> TargetAttributes { get; }

        private bool isIgnoredDuringCommit => this.IgnoreAttributes.Value.Any(ia => ia.IgnoreDuringCommit);

        internal ModelDefinition(MemberInfo member, TypeDefinitionCollection typeLinks)
        {
            this._memberAccessor = Utility.MemberAccessor.Create(member);
            this.Name = member.Name;
            this.MemberType = Utility.ReflectionHelper.GetMemberType(member);
            this.CollectionType = Utility.ReflectionHelper.GetCollectionType(member);
            this._isCollection = null != this.CollectionType;
            if (!this._isCollection)
                this.CollectionType = this.MemberType;
            this.TypeDefinition = typeLinks.GetOrCreate(this.CollectionType);

            this.RelationshipAttributes = new Lazy<IEnumerable<Attribution.Relation>>(() => this.GetAccessorAttributes<Attribution.Relation>());//only
            this.IgnoreAttributes = new Lazy<IEnumerable<Attribution.Ignore>>(() => this.GetAttributes<Attribution.Ignore>());
            this.TargetAttributes = new Lazy<IEnumerable<Attribution.Target>>(() => this.GetAttributes<Attribution.Target>());
        }

        internal void Flatten(DataSet ds, object source, Dictionary<Type, FlattenedCollection> collections)
        {
            if (this.isIgnoredDuringCommit)
                return;

            var thisCollection = null != source ? this.ToCollection(source) : new object[0];
            var collectionAdded = false;

            if (collections.ContainsKey(this.CollectionType))
            {
                // Append to the collection where there is data to append
                if (thisCollection?.Count > 0)
                    collections[this.CollectionType].AddCollection(this, source, thisCollection);
            }
            else
            {
                // Always create the needed collections
                collections.Add(this.CollectionType, new FlattenedCollection(ds, this, source, thisCollection));
                collectionAdded = true;
            }

            foreach (var nm in this.TypeDefinition.NestedModels)
                if (nm.CollectionType != this.CollectionType && null != thisCollection)
                {
                    if (thisCollection.Count > 0)
                    {
                        foreach (var no in thisCollection)
                            nm.Flatten(ds, no, collections);
                    }
                    else if(collectionAdded)//avoid infinite loops, only need to do this for brand new collections
                    {
                        // Flatten against null so that the empty collection is registered. Fixes a bug where is the nested model was entirely empty, the model would be ignored by the commit.
                        nm.Flatten(ds, null, collections);
                    }
                }
        }

        internal IEnumerable<T> GetAccessorAttributes<T>() where T : Attribute
        {
            return this._memberAccessor.Member.GetCustomAttributes<T>();
        }

        internal IEnumerable<T> GetCollectionTypeAttributes<T>() where T : Attribute
        {
            return this.CollectionType.GetCustomAttributes<T>();
        }

        internal IEnumerable<T> GetAttributes<T>() where T : Attribute
        {
            return this.GetAccessorAttributes<T>().Concat(this.GetCollectionTypeAttributes<T>());
        }

        internal ModelLink GetModelTarget(DataSet ds)
        {
            if(null == this._target)
            {
                DataTable targetDt = null;
                foreach (var target in this.GetTargets())
                {
                    targetDt = Utility.DataTableHelpers.GetByNormalisedName(ds, target);
                    if (null != targetDt)
                        break;
                }

                if(null == targetDt)
                {
                    // ** did the table originate from a source?
                    var source = this.GetModelSource(ds);
                    if (null != source)
                    {
                        var attributedTarget = this.TargetAttributes.Value.FirstOrDefault();
                        if (null != attributedTarget)
                        {
                            // ** Found a source and a target attribute - apply the table name
                            targetDt = source.Data;
                            if (!string.IsNullOrEmpty(attributedTarget.To))
                            {
                                var fqName = Utility.QualifiedSqlName.Parse(attributedTarget.To);
                                targetDt.Namespace = fqName.Schema;
                                targetDt.TableName = fqName.Name;
                            }

                            Utility.DataTableHelpers.SetTarget(targetDt, attributedTarget);
                        }
                    }
                }

                if (null != targetDt)
                    this._target = new ModelLink(this, targetDt);
                else
                    this._target = new ModelLink(this, this.AutoGenerateTable(ds)); //No target or source - automatically generate a table for the model
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
                this.SetValue(source, Utility.ReflectionHelper.ListFromList(this.CollectionType, value));
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
                    .Distinct() //inheritance causes duplicates
                    .ToList();

                // When no sources have been defined programatically or via decoration, the type name is used
                if (0 == Ret.Count)
                {
                    if (ModelDefinition.CheckIfAnonymousType(this.CollectionType))
                        throw new ArgumentException($"The type '{this.CollectionType}' couldn't participate in the database model because it is anonymous and defines no sources.");
                    Ret.Add(this.CollectionType.Name);
                }

                // Normalise the source names into fully qualified SQL names
                this._sources = Ret.Select(s => Utility.QualifiedSqlName.Parse(s).ToString()).ToArray();
            }
            return this._sources;
        }

        internal string[] GetTargets()
        {
            if (null == this._targets)
            {
                // ASC prioritised elsewhere in the code, want Target[0] to be the default, so the member should take priority over the collection type etc.
                var Ret = this.GetAttributedTargetNames();

                //// When no targets have been defined programatically or via decoration, the sources are used instead as a fall back
                if (0 == Ret.Count)
                {
                    var sources = this.GetSources();
                    if (sources.Length > 1)
                        throw new InvalidOperationException($"The model member '{this.Name}' ({this.MemberType}) specifies multiple sources and no target. A target table to commit changes into can't be determined.");
                    Ret.AddRange(sources);
                }

                //// When no targets have been defined programatically or via decoration, the type name is used
                //if (0 == Ret.Count)
                //{
                //    if (ModelDefinition.CheckIfAnonymousType(this.CollectionType))
                //        throw new ArgumentException($"The type '{this.CollectionType}' couldn't participate in the database model because it is anonymous and defines no target.");
                //    Ret.Add(this.CollectionType.Name);
                //}

                // Normalise the source names into fully qualified SQL names
                this._targets = Ret.Select(s => Utility.QualifiedSqlName.Parse(s).ToString()).ToArray();
            }
            return this._targets;
        }

        private List<string> GetAttributedTargetNames()
        {
            return this.TargetAttributes.Value
                    .Select(a => a.To)
                    .Where(to => null != to)
                    .Distinct() //inheritance causes duplicates
                    .ToList();
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
            // ** Try to obtain a default target
            var Targets = this.GetTargets();

            if (Targets.Length > 0)
                return Targets[0];

            throw new InvalidOperationException($"A target table for the model member '{this.Name}' ({this.MemberType}) couldn't be determined.");
        }

        private DataTable AutoGenerateTable(DataSet ds)
        {
            var tn = Utility.QualifiedSqlName.Parse(this.GetDefaultTableNameForType());
            var dt = new DataTable(tn.Name, tn.Schema);

            foreach (var member in this.TypeDefinition.PrimitiveMembers)
                if (!dt.Columns.Contains(member.Name))
                    dt.Columns.Add(member.Name, Utility.ReflectionHelper.GetMemberTypeOrGenericType(member));

            ds.Tables.Add(dt);
            return dt;
        }
    }
}
