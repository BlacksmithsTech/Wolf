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

		public static SimpleResultModel<T> CreateSimpleResultModel<T>(params T[] model) where T : class
		{
			return new SimpleResultModel<T>(model);
		}

		/// <summary>
		/// Creates a new result model with no backing change tracking
		/// </summary>
		public ResultModel()
		{
			this._data = new DataSet();
		}

		/// <summary>
		/// Creates a new result model with change tracking from an original source
		/// </summary>
		/// <param name="source">source data for change tracking</param>
		public ResultModel(DataSet source)
		{
			this._data = source;
		}

		internal void TrackChanges()
		{
			// ** Model-first unboxing routine.
			this._data.EnforceConstraints = false;
			foreach (var ml in this.GetModelMembers())
				this.UnboxEnumerable(ml.CollectionType, ml.Collection, this._data);
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
						CollectionType = fi.FieldType.GetElementType(),
						Collection = ((IEnumerable<object>)fi.GetValue(this)).Where(o => null != o)
					})
					.ToArray();
			return this._modelMembers;
		}

		private void UnboxEnumerable(Type t, IEnumerable<object> collection, DataSet ds)
		{
			var tl = this.GetTableForType(t, ds);

			var UnhandledModels = new List<object>(collection);

			// ** Updates and deletes
			foreach(DataRow row in tl.Table.Rows)
			{
				var ModelObject = tl.FindObject(row, collection);

				if(null != ModelObject)
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
			foreach(var ModelObject in UnhandledModels)
			{
				var row = tl.Table.NewRow();
				this.UnboxObject(ModelObject, tl, row);
				tl.Table.Rows.Add(row);
			}
		}

		private void UnboxObject(object o, TypeLink tl, DataRow r)
		{
			foreach(var ml in tl.Members.Where(m => null != m.Column))
			{
				object value = null;

				if(ml.Member is FieldInfo field)
				{
					value = field.GetValue(o);
				}

				r[ml.Column] = value;
			}
		}

		private TypeLink GetTableForType(Type t, DataSet ds)
		{
			if (null == this._typeLinks)
				this._typeLinks = new List<TypeLink>();
			var link = this._typeLinks.FirstOrDefault(tl => t.Equals(tl.Type));

			if(null == link)
			{
				link = new TypeLink(t);
				if (ds.Tables.Contains(link.TableName))
					link.Table = ds.Tables[link.TableName];
				else
					link.Table = this.CreateTableForType(link, ds);

				//TODO: Work out the most effective way of relating objects to rows and assign that as a delegate method
				link.FindRow = link.AlwaysNewRow;
				link.FindObject = link.FindObject_FullEquality;
			}

			return link;
		}

		private DataTable CreateTableForType(TypeLink tl, DataSet ds)
		{
			var dt = new DataTable(tl.TableName);

			foreach(var member in tl.Members)
			{
				if (member.Member is FieldInfo field)
				{
					member.Column = dt.Columns[field.Name];

					if (null == member.Column)
					{
						//TODO: keys
						//TODO: nullables
						//TODO: nested types
						member.Column = dt.Columns.Add(field.Name, field.FieldType);
					}
				}
			}

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
		internal IEnumerable<object> Collection;
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

					if(!r[ml.Column].Equals(value))
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
