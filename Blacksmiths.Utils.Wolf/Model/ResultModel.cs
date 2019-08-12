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
				this.UnboxEnumerable(ml.CollectionType, ml.ToEnumerable(this), this._data);
			this._data.EnforceConstraints = true;
		}

		internal void DataBind(DataSet ds)
		{
			if (null == this._data)
				this._data = new DataSet();

			this._data.Merge(ds);

			this._data.EnforceConstraints = false;
			foreach (var ml in this.GetModelMembers())
				Utility.ReflectionHelper.SetValue(ml.Member, this, this.BoxEnumerable(ml.CollectionType, ml.ToCollection(this), this._data));
			this._data.EnforceConstraints = true;
		}

		private ModelLink[] GetModelMembers()
		{
			if (null == this._modelMembers)
				this._modelMembers = this.GetType()
					.GetFields(BindingFlags.Instance | BindingFlags.Public)
					.Where(fi => fi.FieldType.IsArray)
					.Select(fi => new ModelLink()
					{
						Member = fi,
						CollectionType = fi.FieldType.GetElementType()
					})
					.ToArray();
			return this._modelMembers;
		}

		private void UnboxEnumerable(Type t, IEnumerable<object> collection, DataSet ds)
		{
			var tl = this.GetTableForType(t, ds);

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

		private ICollection<object> BoxEnumerable(Type t, ICollection<object> collection, DataSet ds)
		{
			var tl = this.GetTableForType(t, ds);

			var Result = collection.IsReadOnly ? new List<object>(collection) : collection;
			var UnhandledModels = new List<object>(collection);

			// ** Updates and inserts
			foreach (DataRow row in tl.Table.Rows)
			{
				var ModelObject = tl.FindObject(row, collection);

				if (null != ModelObject)
				{
					// ** Update the row and tally off the object.
					//this.UnboxObject(ModelObject, tl, row);
					UnhandledModels.Remove(ModelObject);
				}
				else
				{
					// ** No Model Object, this row is to be inserted
					Result.Add(this.BoxObject(Activator.CreateInstance(t), tl, row));//TODO: sensible errors for objects which can't be activated simply
				}
			}

			// ** Deletes
			foreach (var ModelObject in UnhandledModels)
				Result.Remove(ModelObject);

			if(Result == collection)
			{
				return Result;
			}
			else
			{
				var a = Array.CreateInstance(t, Result.Count);
				Array.Copy(Result.ToArray(), a, Result.Count);
				return (ICollection<object>)a;
			}
		}

		private object BoxObject(object o, TypeLink tl, DataRow r)
		{
			//TODO: inner nested types

			foreach (var ml in tl.GetLinkedMembers())
				if (r.IsNull(ml.Column))
					Utility.ReflectionHelper.SetValue(ml.Member, o, null);//TODO: check if null is possible for the member.
				else
					Utility.ReflectionHelper.SetValue(ml.Member, o, r[ml.Column]);
			return o;
		}

		private TypeLink GetTableForType(Type t, DataSet ds)
		{
			if (null == this._typeLinks)
				this._typeLinks = new List<TypeLink>();
			var link = this._typeLinks.FirstOrDefault(tl => t.Equals(tl.Type));

			if (null == link)
			{
				link = new TypeLink(t);
				if (ds.Tables.Contains(link.TableName))
					link.Table = ds.Tables[link.TableName];
				else
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
			var dt = new DataTable(tl.TableName);

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
		internal MemberInfo Member;
		internal Type CollectionType;

		internal ICollection<object> ToCollection(object source)
		{
			return (ICollection<object>)Utility.ReflectionHelper.GetValue(Member, source);
		}

		internal IEnumerable<object> ToEnumerable(object source)
		{
			return this.ToCollection(source).Where(o => null != o);
		}

		
	}

	internal sealed class TypeLink
	{
		internal delegate DataRow RowFinder(object o);
		internal delegate object ObjectFinder(DataRow r, IEnumerable<object> collection);

		internal Type Type;
		internal string TableName;
		internal MemberLink[] Members;
		internal DataTable Table;
		internal RowFinder FindRow;
		internal ObjectFinder FindObject;

		internal TypeLink(Type t)
		{
			this.Type = t;
			this.TableName = this.GetTableNameForType(this.Type);
			this.Members = this.Type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).Select(mi => new MemberLink(mi)).ToArray();
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

				foreach (var ml in this.Members.Where(m => null != m.Column))
				{
					object value = null;
					if (ml.Member is FieldInfo field)
					{
						value = field.GetValue(o);
					}

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

		private string GetTableNameForType(Type t)
		{
			//TODO: Flexibility here, e.g. use attribution

			// ** No other table name declared, use the type name.
			if (TypeLink.CheckIfAnonymousType(t))
				throw new ArgumentException($"The type '{t}' couldn't participate in the database model because it is anonymous.");
			return t.Name;
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

	internal sealed class MemberLink
	{
		internal MemberInfo Member;
		internal DataColumn Column;

		internal MemberLink(MemberInfo m)
		{
			this.Member = m;
		}
	}
}
