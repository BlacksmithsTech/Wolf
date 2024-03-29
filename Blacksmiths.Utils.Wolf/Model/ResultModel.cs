﻿/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Model
{
	public sealed class SimpleResultModel<T> : ResultModel
	{
		public List<T> Results;

		public SimpleResultModel()
			: this(null) { }

		public SimpleResultModel(params T[] model)
		{
			if (null != model)
				this.Results = new List<T>(model);
		}
	}

	public class ResultModel
	{
		private DataSet _data;
		private ModelDefinition[] _modelMembers;
		private TypeDefinitionCollection _typeDefinitions;

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
		internal void TrackChanges(DataConnection connection)
		{
			if (null == this._data)
				this._data = new DataSet();

			Utility.Logging.BeginTrace("Flattening object structure");
			var Collections = new Dictionary<Type, FlattenedCollection>();
			foreach (var ml in this.GetTopLevelModelMembers())
				ml.Flatten(this._data, this, Collections);
			Utility.Logging.EndTrace("Flattening object structure");

			Utility.Logging.BeginTrace("Unboxing");
			var relationships = new Queue<IRelationshipDemand>();
			foreach (var collection in Collections.Values)
			{
				this.UnboxEnumerable(connection, collection, relationships);
			}

			Utility.Logging.EndTrace("Unboxing");
			Utility.Logging.BeginTrace("Creating and enabling constraints");

			// ** Create the FKs
			this._data.EnforceConstraints = false;
			while (relationships.Count > 0)
				relationships.Dequeue().CreateForeignKey(this._data);

			try
			{
				this._data.EnforceConstraints = true;
			}
			catch (ConstraintException ce)
			{
				throw new Exceptions.ConstraintException(this._data, ce);
			}
			finally
			{
				Utility.Logging.EndTrace("Creating and enabling constraints");
			}
		}

		/// <summary>
		/// Bind the current model to the data contained in the given DataSet
		/// </summary>
		internal void DataBind(DataSet ds)
		{
			Utility.Logging.BeginTrace("Model databinding");

			//TODO: The reliance here on using the DataSet as the foundation to the boxing is probably aggravating perf costs (exhasibated over large result sets). Should consider directly boxing from DataReaders.
			if (null == this._data)
				this._data = ds;
			else
				this._data.Merge(ds);

			// ** Populate root model objects
			var Relationships = new Queue<MemberRelationshipDemand>();
			foreach (var ml in this.GetTopLevelModelMembers())
				ml.SetValue(this, this.BoxEnumerable(ml, this._data, Relationships, ml.ToCollection(this)));

			// ** Assign related data
			this.SatisfyRelationshipDemands(Relationships);

			Utility.Logging.EndTrace("Model databinding");
		}

		private void SatisfyRelationshipDemands(Queue<MemberRelationshipDemand> Demands)
		{
			Utility.Logging.BeginTrace("Satisfying relationships");

			if (Demands.Count > 0)
			{
				// ** Create a collection which holds both strongly defined model members and any nested members which turn up in the model
				//var ModelLinks = new List<ModelDefinition>(this.GetAllModelMembers());
				var Collections = new Dictionary<Type, System.Collections.IList>();

				foreach (var ml in this.GetTopLevelModelMembers())
					if (!Collections.ContainsKey(ml.CollectionType))
						Collections.Add(ml.CollectionType, ml.ToCollection(this));

				while (Demands.Count > 0)
				{
					var Demand = Demands.Dequeue();
					var ChildModelDef = Demand.ChildModelDefinition;

					if (!Collections.ContainsKey(ChildModelDef.CollectionType))
						Collections.Add(ChildModelDef.CollectionType, this.BoxEnumerable(ChildModelDef, this._data, Demands, null));

					Demand.Satisfy(ChildModelDef.GetModelSource(this._data), Collections[ChildModelDef.CollectionType]);
				}
			}

			Utility.Logging.EndTrace("Satisfying relationships");
		}

		private ModelDefinition[] GetTopLevelModelMembers()
		{
			if (null == this._typeDefinitions)
				this._typeDefinitions = new TypeDefinitionCollection();

			if (null == this._modelMembers)
				this._modelMembers = this.GetType()
					.GetMembers(BindingFlags.Instance | BindingFlags.Public)
					.Where(m => new[] { MemberTypes.Property, MemberTypes.Field }.Contains(m.MemberType))
					.Where(m => typeof(System.Collections.IList).IsAssignableFrom(Utility.ReflectionHelper.GetMemberType(m)))
					.Select(m => new ModelDefinition(m, this._typeDefinitions))
					.ToArray();
			return this._modelMembers;
		}

		private void UnboxEnumerable(DataConnection connection, FlattenedCollection flattened, Queue<IRelationshipDemand> relationships)
		{
			foreach (var range in flattened)
				range.ModelLink.ThrowIfCantUpdate(connection);

			this.UnboxEnumerable(flattened, relationships);
		}

		private void UnboxEnumerable(FlattenedCollection flattened, Queue<IRelationshipDemand> relationships)
		{
			foreach (var dataTable in flattened.Select(r => r.ModelLink.Data).Distinct().OrderBy(dt => dt, new Utility.DataTableComparer()))
			{
				var rangesInThisDataTable = flattened.Where(r => r.ModelLink.Data == dataTable);
				var modelsInThisDataTable = rangesInThisDataTable.SelectMany(r => flattened.GetCollectionRange(r));
				var rowsInThisDataTable = dataTable.Rows.Cast<DataRow>().ToList();

				foreach (var range in rangesInThisDataTable)
				{
					foreach (var model in flattened.GetCollectionRange(range))
					{
						var row = dataTable.Rows.Find(range.ModelLink.GetKeyValues(model));
						if (null != row)
						{
							var changesMade = UnboxObject(model, range.ModelLink, row);
							rowsInThisDataTable.Remove(row);

							//if (changesMade > 0)
							//	Utility.PerfDebuggers.Trace($"[UPDATE] {Utility.DataTableHelpers.GetNormalisedName(dataTable)} : {string.Join(", ", row.ItemArray)}");
						}
						else
						{
							row = range.ModelLink.Data.NewRow();
							UnboxObject(model, range.ModelLink, row);
							range.ModelLink.Data.Rows.Add(row);
							range.ModelLink.RememberAddedRow(row, model);

							//Utility.PerfDebuggers.Trace($"[INSERT] {Utility.DataTableHelpers.GetNormalisedName(dataTable)}: {string.Join(", ", row.ItemArray)}");
						}
					}

					// ** Add member relationship demands across tables/objects
					foreach (var childModelDef in range.ModelLink.ModelDefinition.TypeDefinition.NestedModels)
					{
						var Demand = relationships.OfType<MemberRelationshipDemand>().FirstOrDefault(d => d.ChildModelDefinition.Equals(childModelDef));
						if (null == Demand)
						{
							Demand = new MemberRelationshipDemand(range.ModelLink, childModelDef, null);
							relationships.Enqueue(Demand);
						}
					}

					// ** Add member relationship demands for the same table
					if(range.ModelLink.ModelDefinition.TypeDefinition.SelfRelationships.Any())
					{
						var demand = relationships.OfType<MemberRelationshipDemand>().FirstOrDefault(d => d.ChildModelDefinition.Equals(range.ModelLink.ModelDefinition) && d.ParentModelLink == range.ModelLink);
						if (null == demand)
						{
							// only supports one for now
							demand = new MemberRelationshipDemand(range.ModelLink, range.ModelLink.ModelDefinition, null);
							demand.Relationship = range.ModelLink.ModelDefinition.TypeDefinition.SelfRelationships.FirstOrDefault();
							relationships.Enqueue(demand);
						}
					}

					// ** Add foreign key demands, which don't populate objects but do influence ordering
					foreach (var foreignKey in range.ModelLink.GetForeignKeys())
					{
						var demand = relationships.OfType<ForeignKeyRelationshipDemand>().FirstOrDefault(d => d.ParentTable == range.ModelLink.Data && d.ForeignKey.Equals(foreignKey));
						if (null == demand)
							relationships.Enqueue(new ForeignKeyRelationshipDemand(range.ModelLink.Data, foreignKey));
					}
				}

				foreach (var rowToDelete in rowsInThisDataTable)
				{
					//Utility.PerfDebuggers.Trace($"[DELETE] {Utility.DataTableHelpers.GetNormalisedName(dataTable)}: {string.Join(", ", rowToDelete.ItemArray)}");
					rowToDelete.Delete();
				}
			}

			//         // ** Updates and deletes
			//         foreach (DataRow row in modelLink.Data.Rows)
			//         {
			//             var ModelObject = modelLink.ModelDefinition.TypeDefinition.FindObject(row, UnhandledModels, modelLink.KeyColumns);

			//             if (null != ModelObject)
			//             {
			//                 // ** Update the row and tally off the object.
			//                 UnboxObject(ModelObject, modelLink, row);
			//                 UnhandledModels.Remove(ModelObject);
			//             }
			//             else
			//             {
			//                 // ** No Model Object, this row is to be deleted
			//                 row.Delete();
			//             }
			//         }

			//         // ** Inserts
			//         foreach (var ModelObject in UnhandledModels)
			//         {
			//             var row = modelLink.Data.NewRow();
			//             UnboxObject(ModelObject, modelLink, row);
			//             modelLink.Data.Rows.Add(row);
			//	modelLink.RememberAddedRow(row, ModelObject);
			//         }

			////// ** Relationship demands
			//foreach (var childModelDef in modelLink.ModelDefinition.TypeDefinition.NestedModels)
			//{
			//	var Demand = relationships.FirstOrDefault(d => d.ChildModelDefinition.Equals(childModelDef));
			//	if (null == Demand)
			//	{
			//		Demand = new MemberRelationshipDemand(modelLink, childModelDef, null);
			//		relationships.Enqueue(Demand);
			//	}
			//}
		}

		private static Func<object, object, bool> enumValueComparer = (columnValue, modelValue) => ((string)columnValue).Equals(modelValue.ToString(), StringComparison.CurrentCultureIgnoreCase);
		private static Func<object, object, bool> standardValueComparer = (columnValue, modelValue) => columnValue.Equals(modelValue);

		internal static int UnboxObject(object o, ModelLink modelLink, DataRow r)
		{
			int changesMade = 0;

			// ** Primitives
			foreach (var ml in modelLink.Members)
			{
				if (ml.Column.AutoIncrement && r.RowState == DataRowState.Detached)
					continue; //Leave the ADO.NET AutoIncrement value alone

				var v = ml.GetValue(o);

				if (r.IsNull(ml.Column))
				{
					if (null != v)
					{
						r[ml.Column] = v;
						changesMade++;
					}
				}
				else
				{
					Func<object, object, bool> valueComparer;
					if (ml.MemberElementType.IsEnum && ml.Column.DataType == typeof(string))
						valueComparer = enumValueComparer;
					else
						valueComparer = standardValueComparer;

					if (!valueComparer(r[ml.Column], v))
					{
						if (null == v)
							r[ml.Column] = DBNull.Value;
						else
							r[ml.Column] = v;
						changesMade++;
					}
				}
			}

			return changesMade;
		}

		private System.Collections.IList BoxEnumerable(ModelDefinition modelDef, DataSet ds, Queue<MemberRelationshipDemand> relationships, System.Collections.IList sourceCollection)
		{
			var modelLink = modelDef.GetModelSource(ds);
			IEnumerable<object> castedCollection;

			if (null == modelLink)
				return sourceCollection; //No table data source, return original source as presented unchanged
			else if (null == sourceCollection)
				sourceCollection = Utility.ReflectionHelper.ListFromList(modelLink.ModelDefinition.CollectionType); // Null original data, forge an empty array

			castedCollection = sourceCollection.Cast<object>();
			var Result = modelDef.MemberType.IsArray ? new List<object>(castedCollection) : sourceCollection;
			var UnhandledModels = new List<object>(castedCollection);
			var sourceCollectionIsEmpty = 0 == sourceCollection.Count;

			// ** Updates and inserts
			foreach (DataRow row in modelLink.Data.Rows)
			{
				var ModelObject = !sourceCollectionIsEmpty ? modelDef.TypeDefinition.FindObject(row, castedCollection, modelLink.KeyColumns) : null;

				if (null != ModelObject)
				{
					// ** Update the row and tally off the object.
					BoxObject(ModelObject, modelLink, row);
					UnhandledModels.Remove(ModelObject);
				}
				else
				{
					// ** No Model Object, this row is to be inserted
					Result.Add(BoxObject(Activator.CreateInstance(modelDef.CollectionType), modelLink, row));//TODO: sensible errors for objects which can't be activated simply
				}
			}

			// ** Deletes
			foreach (var ModelObject in UnhandledModels)
				Result?.Remove(ModelObject);

			if (modelDef.MemberType.IsArray)
				Result = Utility.ReflectionHelper.ArrayFromList(modelDef.CollectionType, Result);

			// ** Relationship demands
			foreach (var childModelDef in modelDef.TypeDefinition.NestedModels)
			{
				var Demand = relationships.FirstOrDefault(d => d.ChildModelDefinition.Equals(childModelDef));
				if (null == Demand)
				{
					Demand = new MemberRelationshipDemand(modelLink, childModelDef, Result);
					relationships.Enqueue(Demand);
				}
			}

			return Result;
		}

		internal static object BoxObject(object o, ModelLink modelLink, DataRow r)
		{
			foreach (var ml in modelLink.Members)
				if (r.IsNull(ml.Column))
					SetModelValue(ml, o, null);
				else
					SetModelValue(ml, o, r[ml.Column]);
			return o;
		}

		internal static void SetModelValue(MemberLink ml, object model, object value)
		{
			if (!Utility.ReflectionHelper.IsAssignable(ml.Column.DataType, ml.MemberType))
				throw new ArgumentException($"Incompatible type '{ml.Column.DataType.FullName}'->'{ml.MemberType.FullName}' whilst assigning '{Utility.StringHelpers.GetFullColumnName(ml.Column)}'->'{Utility.StringHelpers.GetFullMemberName(ml.Member)}'");
			ml.SetValue(model, value);
		}

		internal bool IsSameDs(DataSet ds)
		{
			return this._data == ds;
		}

		internal void AutoGetSchema(DataConnection connection)
		{
			if (null != this._data)
				foreach (DataTable dt in this._data.Tables)
					if (0 == dt.PrimaryKey.Length)
						connection.FetchSchema(dt);
		}

		internal DataSet GetDataSet(DataConnection connection)
		{
			this.TrackChanges(connection);
			return this._data;
		}

		internal DataSet GetCopiedDataSet(DataConnection connection)
		{
			return this.GetDataSet(connection).Copy();
		}

		internal void AcceptChanges()
		{
			this._data.AcceptChanges();
		}
	}
}
