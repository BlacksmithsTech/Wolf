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
		internal void TrackChanges()
		{
			if (null == this._data)
				this._data = new DataSet();

            Utility.PerfDebuggers.BeginTrace("Flattening object structure");
            var Collections = new Dictionary<Type, FlattenedCollection>();
            foreach (var ml in this.GetTopLevelModelMembers())
                ml.Flatten(this._data, this, Collections);
            Utility.PerfDebuggers.EndTrace("Flattening object structure");

            this._data.EnforceConstraints = false;
            foreach (var collection in Collections.Values)
                this.UnboxEnumerable(collection);
            this._data.EnforceConstraints = true;
		}

        /// <summary>
        /// Bind the current model to the data contained in the given DataSet
        /// </summary>
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
			foreach (var ml in this.GetTopLevelModelMembers())
				ml.SetValue(this, this.BoxEnumerable(ml, this._data, Relationships, ml.ToCollection(this)));

            // ** Assign related data
            this.SatisfyRelationshipDemands(Relationships);

            Utility.PerfDebuggers.EndTrace("Model databinding");
        }

        private void SatisfyRelationshipDemands(Queue<MemberRelationshipDemand> Demands)
		{
            Utility.PerfDebuggers.BeginTrace("Satisfying relationships");

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

            Utility.PerfDebuggers.EndTrace("Satisfying relationships");
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

		private void UnboxEnumerable(FlattenedCollection flattened)
		{
            foreach (var range in flattened)
            {
                range.ModelLink.ThrowIfCantUpdate();
                this.UnboxEnumerable(range.ModelLink, flattened.GetCollectionRange(range));
            }
		}

        private void UnboxEnumerable(ModelLink modelLink, IEnumerable<object> collection)
        {
            var UnhandledModels = new List<object>(collection);

            // ** Updates and deletes
            foreach (DataRow row in modelLink.Data.Rows)
            {
                var ModelObject = modelLink.ModelDefinition.TypeDefinition.FindObject(row, UnhandledModels, modelLink.KeyColumns);

                if (null != ModelObject)
                {
                    // ** Update the row and tally off the object.
                    UnboxObject(ModelObject, modelLink, row);
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
                var row = modelLink.Data.NewRow();
                UnboxObject(ModelObject, modelLink, row);
                modelLink.Data.Rows.Add(row);
				modelLink.RememberAddedRow(row, ModelObject);
            }
        }

		internal static void UnboxObject(object o, ModelLink modelLink, DataRow r)
		{
            // ** Primitives
			foreach (var ml in modelLink.Members)
			{
                var v = ml.GetValue(o);

                if(r.IsNull(ml.Column))
                {
                    if (null != v)
                        r[ml.Column] = v;
                }
                else if(!r[ml.Column].Equals(v))
                {
                    r[ml.Column] = v;
                }
			}
		}

		private System.Collections.IList CreateListOfType(Type collectionType)
		{
			var t = typeof(List<>).MakeGenericType(collectionType);
			return (System.Collections.IList)Activator.CreateInstance(t);
		}

		private System.Collections.IList BoxEnumerable(ModelDefinition modelDef, DataSet ds, Queue<MemberRelationshipDemand> relationships, System.Collections.IList sourceCollection)
		{
            var modelLink = modelDef.GetModelSource(ds);
			IEnumerable<object> castedCollection;

			if (null == modelLink)
				return sourceCollection; //No table data source, return original source as presented unchanged
			else if (null == sourceCollection)
				sourceCollection = this.CreateListOfType(modelLink.ModelDefinition.CollectionType); // Null original data, forge an empty array

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
            foreach (var childModelDef in modelDef.NestedModels)
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
}
