/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Model
{
	public sealed class SimpleResultModel<T> : ResultModel
	{
		public T[] Results;

		public SimpleResultModel(params T[] model)
		{
			this.Results = model;
		}
	}

	public class ResultModel
	{
		private DataSet _data;

		private List<TypeLink> _typeLinks;
		private ModelLink[] _modelMembers;

		public static SimpleResultModel<T> CreateSimpleResultModel<T>(params T[] model) where T : class, new()
		{
			return new SimpleResultModel<T>(model);
		}

		/// <summary>
		/// Creates a new result model with no backing change tracking
		/// </summary>
		public ResultModel()
		{
		}

		/// <summary>
		/// Creates a new result model with change tracking from an original source
		/// </summary>
		/// <param name="source">source data for change tracking</param>
		public ResultModel(DataSet source)
		{
			this._data = source;
		}

		/// <summary>
		/// Causes the underlying change tracking provided by a DataSet to be updated from the model
		/// </summary>
		internal void TrackChanges()
		{
			if (null == this._data)
				this._data = new DataSet();

			this._data.EnforceConstraints = false;
			foreach (var ml in this.GetModelMembers())
				this.UnboxEnumerable(ml, this._data);
			this._data.EnforceConstraints = true;
		}

		internal void DataBind(DataSet ds)
		{
			if (null == this._data)
				this._data = new DataSet();

			this._data.Merge(ds);

			this._data.EnforceConstraints = false;
			foreach (var ml in this.GetModelMembers())
				Utility.ReflectionHelper.SetValue(ml.Member, this, this.BoxEnumerable(ml, this._data));
			this._data.EnforceConstraints = true;
		}

		private ModelLink[] GetModelMembers()
		{
			if (null == this._modelMembers)
				this._modelMembers = this.GetType()
					.GetMembers(BindingFlags.Instance | BindingFlags.Public)
					.Where(m => new[] { MemberTypes.Property, MemberTypes.Field }.Contains(m.MemberType))
					.Where(m => typeof(System.Collections.IList).IsAssignableFrom(Utility.ReflectionHelper.GetMemberType(m)))
					.Select(m => new ModelLink()
					{
						Member = m,
						CollectionType = Utility.ReflectionHelper.GetCollectionType(m)
					})
					.ToArray();
			return this._modelMembers;
		}

		private void UnboxEnumerable(ModelLink ml, DataSet ds)
		{
			var tl = this.GetTableForType(ml, ds);
			var collection = ml.ToCollection(this).Cast<object>();
			var UnhandledModels = new List<object>(collection);

			// ** Updates and deletes
			foreach (DataRow row in tl.Table.Rows)
			{
				var ModelObject = tl.FindObject(row, collection);

				if (null != ModelObject)
				{
					// ** Update the row and tally off the object.
					this.UnboxObject(ModelObject, tl, row);
					UnhandledModels.Remove(ModelObject);
				}
				else
				{
					// ** No Model Object, this row is to be deleted
					row.Delete();
				}
			}

			// ** Inserts
			foreach (var ModelObject in UnhandledModels)
			{
				var row = tl.Table.NewRow();
				this.UnboxObject(ModelObject, tl, row);
				tl.Table.Rows.Add(row);
			}
		}

		private void UnboxObject(object o, TypeLink tl, DataRow r)
		{
			foreach (var ml in tl.GetLinkedMembers())
			{
				r[ml.Column] = Utility.ReflectionHelper.GetValue(ml.Member, o);
			}
		}

		private System.Collections.IList BoxEnumerable(ModelLink ml, DataSet ds)
		{
			var tl = this.GetTableForType(ml, ds);
			var collection = ml.ToCollection(this);
			var castedCollection = collection.Cast<object>();
			var Result = collection.IsFixedSize ? new List<object>(castedCollection) : collection;
			var UnhandledModels = new List<object>(castedCollection);

			// ** Updates and inserts
			foreach (DataRow row in tl.Table.Rows)
			{
				var ModelObject = tl.FindObject(row, castedCollection);

				if (null != ModelObject)
				{
					// ** Update the row and tally off the object.
					this.BoxObject(ModelObject, tl, row);
					UnhandledModels.Remove(ModelObject);
				}
				else
				{
					// ** No Model Object, this row is to be inserted
					Result.Add(this.BoxObject(Activator.CreateInstance(ml.CollectionType), tl, row));//TODO: sensible errors for objects which can't be activated simply
				}
			}

			// ** Deletes
			foreach (var ModelObject in UnhandledModels)
				Result.Remove(ModelObject);

			if (Result == collection)
			{
				return Result;
			}
			else
			{
				var a = Array.CreateInstance(ml.CollectionType, Result.Count);
				Array.Copy(Result.Cast<object>().ToArray(), a, Result.Count);
				return (System.Collections.IList)a;
			}
		}

		private object BoxObject(object o, TypeLink tl, DataRow r)
		{
			//TODO: inner nested types

			foreach (var ml in tl.GetLinkedMembers())
				if (r.IsNull(ml.Column))
					this.SetModelValue(ml, o, null);
				else
					this.SetModelValue(ml, o, r[ml.Column]);

			return o;
		}

		private void SetModelValue(MemberLink ml, object model, object value)
		{
			if (!ml.Column.DataType.IsAssignableFrom(ml.MemberType))
				throw new ArgumentException($"Incompatible type '{ml.Column.DataType.FullName}'->'{ml.MemberType.FullName}' whilst assigning '{Utility.StringHelpers.GetFullColumnName(ml.Column)}'->'{Utility.StringHelpers.GetFullMemberName(ml.Member)}'");

			Utility.ReflectionHelper.SetValue(ml.Member, model, value);
		}
		private TypeLink GetTableForType(ModelLink ml, DataSet ds)
		{
			if (null == this._typeLinks)
				this._typeLinks = new List<TypeLink>();
			var link = this._typeLinks.FirstOrDefault(tl => ml.CollectionType.Equals(tl.Type));

			if (null == link)
			{
				link = new TypeLink(ml);
				foreach (var source in ml.GetSources().Select(s => Utility.StringHelpers.GetQualifiedSpName(s)))
					if (ds.Tables.Contains(source.Name, source.Schema))
					{
						link.Table = ds.Tables[source.Name, source.Schema];
						break;
					}

				if (null == link.Table)
					link.Table = this.CreateTableForType(link, ds);

				this.LinkColumns(link, link.Table);

				//TODO: Work out the most effective way of relating objects to rows and assign that as a delegate method
				link.FindRow = link.AlwaysNewRow;
				link.FindObject = link.FindObject_FullEquality;

				this._typeLinks.Add(link);
			}

			return link;
		}

		private void LinkColumns(TypeLink tl, DataTable dt)
		{
			foreach (var member in tl.Members)
			{
				if (null == member.Column)
					member.Column = dt.Columns[member.Member.Name];
			}
		}

		private DataTable CreateTableForType(TypeLink tl, DataSet ds)
		{
			var tn = Utility.StringHelpers.GetQualifiedSpName(tl.DefaultTableName);
			var dt = new DataTable(tn.Name, tn.Schema);

			foreach (var member in tl.Members)
				if (!dt.Columns.Contains(member.Member.Name))
					dt.Columns.Add(member.Member.Name, Utility.ReflectionHelper.GetMemberType(member.Member));

			ds.Tables.Add(dt);
			return dt;
		}

		internal DataSet GetDataSet()
		{
			this.TrackChanges();
			return this._data;
		}

		internal DataSet GetCopiedDataSet()
		{
			return this.GetDataSet().Copy();
		}
	}

	internal sealed class ModelLink
	{
		private string[] _Sources;

		internal MemberInfo Member;
		internal Type CollectionType;

		internal System.Collections.IList ToCollection(object source)
		{
			return (System.Collections.IList)Utility.ReflectionHelper.GetValue(Member, source);
		}

		internal IEnumerable<object> ToEnumerable(object source)
		{
			return this.ToCollection(source).Cast<object>().Where(o => null != o);
		}

		internal string[] GetSources()
		{
			if (null == this._Sources)
			{
				// Asc order sensitive.
				var Ret = this.Member.GetCustomAttributes<Attribution.Source>()
					.Concat(this.CollectionType.GetCustomAttributes<Attribution.Source>())
					.Select(a => a.From)
					.ToList();

				// When no sources have been defined programatically or via decoration, the type name is used
				if (0 == Ret.Count)
				{
					if (ModelLink.CheckIfAnonymousType(this.CollectionType))
						throw new ArgumentException($"The type '{this.CollectionType}' couldn't participate in the database model because it is anonymous and defines no sources.");
					Ret.Add(this.CollectionType.Name);
				}

				this._Sources = Ret.ToArray();
			}
			return this._Sources;
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
	}

	internal sealed class TypeLink
	{
		internal delegate DataRow RowFinder(object o);
		internal delegate object ObjectFinder(DataRow r, IEnumerable<object> collection);

		internal ModelLink ModelLink;
		internal Type Type;
		internal string DefaultTableName;
		internal MemberLink[] Members;
		internal DataTable Table;
		internal RowFinder FindRow;
		internal ObjectFinder FindObject;

		internal TypeLink(ModelLink ml)
		{
			this.ModelLink = ml;
			this.Type = ml.CollectionType;
			this.DefaultTableName = this.GetDefaultTableNameForType(ml);
			this.Members = this.Type
				.GetMembers(BindingFlags.Public | BindingFlags.Instance)
				.Where(m => new[] { MemberTypes.Property, MemberTypes.Field }.Contains(m.MemberType))
				.Select(m => new MemberLink(m))
				.ToArray();
		}

		/// <summary>
		/// Gets members that have been successfully tracked/linked against a datatable column
		/// </summary>
		internal IEnumerable<MemberLink> GetLinkedMembers()
		{
			return this.Members.Where(m => null != m.Column);
		}

		internal object FindObject_FullEquality(DataRow r, IEnumerable<object> collection)
		{
			foreach (var o in collection)
			{
				bool Equal = true;

				foreach (var ml in this.GetLinkedMembers())
				{
					object value = Utility.ReflectionHelper.GetValue(ml.Member, o);

					if (!r[ml.Column].Equals(value))
					{
						Equal = false;
						break;
					}
				}

				if (Equal)
					return o;
			}

			return null;
		}

		internal DataRow AlwaysNewRow(object o)
		{
			return Table.NewRow();
		}

		private string GetDefaultTableNameForType(ModelLink ml)
		{
			var t = ml.CollectionType;

			// ** Try to obtain a default source
			var Sources = ml.GetSources();

			if (1 == Sources.Length)
				return Sources[0]; // Single source, assume this is the table name

			//TODO: Default via further attribution?

			if (Sources.Length > 1)
				throw new InvalidOperationException($"The model member '{ml.Member.Name}' ({Utility.ReflectionHelper.GetMemberType(ml.Member)}) specifies multiple sources. A target table to commit changes into can't be determined.");
			else
				throw new InvalidOperationException($"A target table for the model member '{ml.Member.Name}' ({Utility.ReflectionHelper.GetMemberType(ml.Member)}) couldn't be determined.");
		}
	}

	internal sealed class MemberLink
	{
		internal MemberInfo Member;
		internal DataColumn Column;
		internal Type MemberType;

		internal MemberLink(MemberInfo m)
		{
			this.Member = m;
			this.MemberType = Utility.ReflectionHelper.GetMemberType(this.Member);
		}
	}
}
