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

		//private List<TypeLink> _typeLinks;
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
			Utility.PerfDebuggers.BeginTrace("Model databinding");

			//TODO: The reliance here on using the DataSet as the foundation to the boxing is probably aggravating perf costs (exhasibated over large result sets). Should consider directly boxing from DataReaders.
			if (null == this._data)
				this._data = ds;
			else
				this._data.Merge(ds);

			// ** Populate root model objects
			var Relationships = new Queue<MemberRelationshipDemand>();
			foreach (var ml in this.GetModelMembers())
				ml.SetValue(this, this.BoxEnumerable(ml, this._data, Relationships, ml.ToCollection(this)));

			// ** Assign related data
			this.SatisfyRelationshipDemands(Relationships);

			Utility.PerfDebuggers.EndTrace("Model databinding");
		}

		private void SatisfyRelationshipDemands(Queue<MemberRelationshipDemand> Demands)
		{
			if (Demands.Count > 0)
			{
				// ** Create a collection which holds both strongly defined model members and any nested members which turn up in the model
				var ModelLinks = new List<ModelLink>(this.GetModelMembers());
				var Collections = new Dictionary<Type, System.Collections.IList>();

				foreach (var ml in ModelLinks)
					if (!Collections.ContainsKey(ml.CollectionType))
						Collections.Add(ml.CollectionType, ml.ToCollection(this));

				while (Demands.Count > 0)
				{
					var Demand = Demands.Dequeue();
					var ModelLink = ModelLinks.FirstOrDefault(ml => ml.MemberEquals(Demand.Relationship.Member));
					
					if(null == ModelLink)
					{
						// ** Not seen this model member before (probably nested)
						ModelLink = new ModelLink(Demand.Relationship.Member);
						ModelLinks.Add(ModelLink);
					}

					if (!Collections.ContainsKey(ModelLink.CollectionType))
						Collections.Add(ModelLink.CollectionType, this.BoxEnumerable(ModelLink, this._data, Demands, null));

					Demand.SatisfyFrom(ModelLink.TypeLink, Collections[ModelLink.CollectionType]);
				}
			}
		}

		private ModelLink[] GetModelMembers()
		{
			if (null == this._modelMembers)
				this._modelMembers = this.GetType()
					.GetMembers(BindingFlags.Instance | BindingFlags.Public)
					.Where(m => new[] { MemberTypes.Property, MemberTypes.Field }.Contains(m.MemberType))
					.Where(m => typeof(System.Collections.IList).IsAssignableFrom(Utility.ReflectionHelper.GetMemberType(m)))
					.Select(m => new ModelLink(m))
					.ToArray();
			return this._modelMembers;
		}

		private void UnboxEnumerable(ModelLink ml, DataSet ds)
		{
			var tl = this.GetTableForType(ml, ds, true);
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

		private System.Collections.IList BoxEnumerable(ModelLink ml, DataSet ds, Queue<MemberRelationshipDemand> relationships, System.Collections.IList sourceCollection)
		{
			var tl = this.GetTableForType(ml, ds, false);
			IEnumerable<object> castedCollection;

			if (null == tl.Table)
				return sourceCollection; //No table data source, return original source as presented unchanged
			else if (null == sourceCollection)
				sourceCollection = new List<object>(); // Null original data, forge an empty array

			castedCollection = sourceCollection.Cast<object>();
			var Result = ml.MemberType.IsArray ? new List<object>(castedCollection) : sourceCollection;
			var UnhandledModels = new List<object>(castedCollection);

			// ** Updates and inserts
			foreach (DataRow row in tl.Table.Rows)
			{
				var ModelObject = tl.FindObject(row, castedCollection);

				if (null != ModelObject)
				{
					// ** Update the row and tally off the object.
					this.BoxObject(ModelObject, tl, row, relationships);
					UnhandledModels.Remove(ModelObject);
				}
				else
				{
					// ** No Model Object, this row is to be inserted
					Result.Add(this.BoxObject(Activator.CreateInstance(ml.CollectionType), tl, row, relationships));//TODO: sensible errors for objects which can't be activated simply
				}
			}

			// ** Deletes
			foreach (var ModelObject in UnhandledModels)
				Result?.Remove(ModelObject);

			if (ml.MemberType.IsArray)
			{
				return Utility.ReflectionHelper.ArrayFromList(ml.CollectionType, Result);
			}
			else
			{
				return Result;
			}
		}

		private object BoxObject(object o, TypeLink tl, DataRow r, Queue<MemberRelationshipDemand> relationships)
		{
			foreach (var tlr in tl.Relationships)
			{
				var Demand = relationships.FirstOrDefault(d => d.Relationship.Equals(tlr));
				if(null == Demand)
				{
					Demand = new MemberRelationshipDemand(tlr);
					relationships.Enqueue(Demand);
				}
				Demand.Instances.Enqueue(o);
			}

			foreach (var ml in tl.GetLinkedMembers())
				if (r.IsNull(ml.Column))
					this.SetModelValue(ml, o, null);
				else
					this.SetModelValue(ml, o, r[ml.Column]);

			return o;
		}

		private void SetModelValue(MemberLink ml, object model, object value)
		{
			if (!Utility.ReflectionHelper.IsAssignable(ml.Column.DataType, ml.MemberType))
				throw new ArgumentException($"Incompatible type '{ml.Column.DataType.FullName}'->'{ml.MemberType.FullName}' whilst assigning '{Utility.StringHelpers.GetFullColumnName(ml.Column)}'->'{Utility.StringHelpers.GetFullMemberName(ml.Member)}'");

			Utility.ReflectionHelper.SetValue(ml.Member, model, value);
		}
		private TypeLink GetTableForType(ModelLink ml, DataSet ds, bool AutoCreate)
		{
			if (null == ml.TypeLink)
				ml.CreateTypeLink(ds, AutoCreate);
			return ml.TypeLink;
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

		internal void AcceptChanges()
		{
			this._data.AcceptChanges();
		}
	}

	internal sealed class ModelLink
	{
		private string[] _Sources;

		private MemberInfo Member;
		internal string Name { get; private set; }
		internal Type MemberType { get; private set; }
		internal Type CollectionType { get; private set;}

		internal TypeLink TypeLink { get; private set; }

		internal ModelLink(MemberInfo mi)
		{
			this.Member = mi;
			this.Name = mi.Name;
			this.MemberType = Utility.ReflectionHelper.GetMemberType(mi);
			this.CollectionType = Utility.ReflectionHelper.GetCollectionType(mi);
		}

		internal bool MemberEquals(MemberInfo mi)
		{
			return this.Member.Equals(mi);
		}

		internal System.Collections.IList ToCollection(object source)
		{
			return (System.Collections.IList)Utility.ReflectionHelper.GetValue(Member, source);
		}

		internal void SetValue(object source, object value)
		{
			Utility.ReflectionHelper.SetValue(this.Member, source, value);
		}

		//internal IEnumerable<object> ToEnumerable(object source)
		//{
		//	return this.ToCollection(source).Cast<object>().Where(o => null != o);
		//}

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

		internal void CreateTypeLink(DataSet ds, bool AutoCreate)
		{
			var link = new TypeLink(this);
			foreach (var source in this.GetSources().Select(s => Utility.StringHelpers.GetQualifiedSpName(s)))
				if (ds.Tables.Contains(source.Name, source.Schema))
				{
					link.Table = ds.Tables[source.Name, source.Schema];
					break;
				}

			if (null == link.Table && AutoCreate)
				link.Table = this.CreateTableForType(link, ds);

			this.LinkColumns(link, link.Table);

			//TODO: Work out the most effective way of relating objects to rows and assign that as a delegate method
			link.FindRow = link.AlwaysNewRow;
			link.FindObject = link.FindObject_FullEquality;

			this.TypeLink = link;
		}

		private void LinkColumns(TypeLink tl, DataTable dt)
		{
			if (null != dt)
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
	}

	internal sealed class TypeLink
	{
		internal delegate DataRow RowFinder(object o);
		internal delegate object ObjectFinder(DataRow r, IEnumerable<object> collection);

		internal MemberLink this[string Name]
		{
			get
			{
				return this.Members.FirstOrDefault(m => m.Member.Name.Equals(Name));
			}
		}

		//internal ModelLink ModelLink;
		internal Type Type;
		internal string DefaultTableName;
		internal MemberLink[] Members;
		internal MemberRelationship[] Relationships;
		internal DataTable Table;//this can be null if no source table in the results can be located
		internal RowFinder FindRow;
		internal ObjectFinder FindObject;

		internal TypeLink(ModelLink ml)
		{
			//this.ModelLink = ml;
			this.Type = ml.CollectionType;
			this.DefaultTableName = this.GetDefaultTableNameForType(ml);

			var ReflectedMembers = this.Type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
				.Where(m => new[] { MemberTypes.Property, MemberTypes.Field }.Contains(m.MemberType))
				.ToArray();

			this.Members = ReflectedMembers
				.Where(m => Utility.ReflectionHelper.IsPrimitive(Utility.ReflectionHelper.GetMemberType(m)))
				.Select(m => new MemberLink(m))
				.ToArray();

			this.Relationships = ReflectedMembers
				.Where(m => !Utility.ReflectionHelper.IsPrimitive(Utility.ReflectionHelper.GetMemberType(m)))
				.Select(m => new MemberRelationship(this, m))
				.ToArray();
		}

		internal bool HasMember(string Name)
		{
			return this.Members.Any(m => m.Member.Name.Equals(Name));
		}

		internal Dictionary<string, object> GetValues(object source, IEnumerable<string> MemberNames)
		{
			var Ret = new Dictionary<string, object>();
			foreach (var MemberName in MemberNames)
				Ret.Add(MemberName, this[MemberName].GetValue(source));
			return Ret;
		}

		internal bool CompareValues(object source, string[] MemberNames, object[] ComparisonValues)
		{
			for (int i = 0; i < MemberNames.Length; i++)
				if (!this[MemberNames[i]].GetValue(source).Equals(ComparisonValues[i]))
					return false;
			return true;
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
				throw new InvalidOperationException($"The model member '{ml.Name}' ({ml.MemberType}) specifies multiple sources. A target table to commit changes into can't be determined.");
			else
				throw new InvalidOperationException($"A target table for the model member '{ml.Name}' ({ml.MemberType}) couldn't be determined.");
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

		internal object GetValue(object source)
		{
			return Utility.ReflectionHelper.GetValue(this.Member, source);
		}
	}

	internal sealed class MemberRelationship
	{
		internal TypeLink ParentTypeLink;
		internal MemberInfo Member;
		internal Type MemberType;
		internal Type CollectionType;

		internal MemberRelationship(TypeLink parentTl, MemberInfo m)
		{
			this.ParentTypeLink = parentTl;
			this.Member = m;
			this.MemberType = Utility.ReflectionHelper.GetMemberType(this.Member);
			this.CollectionType = Utility.ReflectionHelper.GetCollectionType(this.Member) ?? Utility.ReflectionHelper.GetMemberType(this.Member);
		}

		internal void SetValue(object source, System.Collections.IList value)
		{
			if (this.MemberType.IsArray)
				Utility.ReflectionHelper.SetValue(this.Member, source, Utility.ReflectionHelper.ArrayFromList(this.CollectionType, value));
			else
				Utility.ReflectionHelper.SetValue(this.Member, source, value);
		}
	}

	internal sealed class MemberRelationshipDemand
	{
		internal MemberRelationship Relationship;
		internal Queue<object> Instances = new Queue<object>();

		internal MemberRelationshipDemand(MemberRelationship r)
		{
			this.Relationship = r;
		}

		internal void SatisfyFrom(TypeLink ChildTypeLink, System.Collections.IList sourceCollection)
		{
			if (null == sourceCollection)
			{
				// No data available to satisfy the relationship. Clear all instances (finished)
				this.Instances.Clear();
				return;
			}

			//Fail. This is taking forever to execute (seconds)
			//Suspect the .NET reflection calls to GetValue and SetValue are slow. Need to optimise and think carefully about how FKs work 1:1, 1:X and X:X
			//See https://mattwarren.org/2016/12/14/Why-is-Reflection-slow/ for techniques to improve the lookup

			//var Relation = this.FindFirstValidRelationship(ChildTypeLink);
			//var sourceDiminishing = new List<object>(sourceCollection.Cast<object>());

			//while (this.Instances.Count > 0)
			//{
			//	var Instance = this.Instances.Dequeue();
			//	this.FilterCollectionForInstance(ChildTypeLink, Instance, Relation, sourceDiminishing);
			//	//this.Relationship.SetValue(Instance, this.FilterCollectionForInstance(ChildTypeLink, Instance, Relation, sourceDiminishing));
			//}

		}

		//private System.Collections.IList FilterCollectionForInstance(TypeLink ChildTypeLink, object ParentInstance, Attribution.Relation Relation, System.Collections.IList sourceCollection)
		//{
		//	if (null == sourceCollection // Nothing to filter on
		//		|| null == Relation) // Or no definition for what to filter by
		//		return sourceCollection;

		//	//TODO: Work directly onto the target collection if possible.For now, always rebuild the collection.
		//	var ParentKeyValues = this.Relationship.ParentTypeLink.GetValues(ParentInstance, Relation.ParentFieldNames).Values.ToArray();
		//	var Result = new List<object>();
		//	for(int i = 0; i < sourceCollection.Count; i++)
		//	{
		//		var ChildInstance = sourceCollection[i];
		//		if(ChildTypeLink.CompareValues(ChildInstance, Relation.ChildFieldNames, ParentKeyValues))
		//		{
		//			sourceCollection.RemoveAt(i);
		//			i--;
		//		}
		//	}

		//	return Result;
		//}

		private Attribution.Relation FindFirstValidRelationship(TypeLink ChildTypeLink)
		{
			foreach(var Relation in this.Relationship.Member.GetCustomAttributes<Attribution.Relation>(true))
			{
				if(Relation.IsSane()
					&& Relation.ParentFieldNames.All(fn => this.Relationship.ParentTypeLink.HasMember(fn))
					&& Relation.ChildFieldNames.All(fn => ChildTypeLink.HasMember(fn)))
				{
					return Relation;
				}
			}
			return null;
		}
	}
}
