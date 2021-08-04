/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace Blacksmiths.Utils.Wolf
{
	public interface IDataConnection
	{
        /// <summary>
        /// Creates a new data request object, which you can use to batch up a number of database commands.
        /// </summary>
        /// <returns></returns>
		DataRequest NewRequest(Action<IDataRequestOptions> setOptions = null);

        /// <summary>
        /// Specifies a data model as a Wolf result model to perform an action with
        /// </summary>
        /// <param name="model">A Wolf result model</param>
        /// <returns>Fluent model action</returns>
        IFluentModelAction WithModel(Model.ResultModel model);

        /// <summary>
        /// Specifies a data model as ADO.NET DataTables to perform an action with
        /// </summary>
        /// <param name="dataTables">One or more ADO.NET DataTables which provide the model</param>
        /// <returns>Fluent model action</returns>
        IFluentModelAction WithModel(DataTable[] dataTables);

        /// <summary>
        /// Specifies a data model as an ADO.NET DataSet to perform an action with
        /// </summary>
        /// <param name="dataSet">An ADO.NET DataSet which provides the model</param>
        /// <returns>Fluent model action</returns>
        IFluentModelAction WithModel(DataSet dataSet);

        /// <summary>
        /// Specifies a data model as objects to perform an action with
        /// </summary>
        /// <typeparam name="T">Type of objects which represent the model</typeparam>
        /// <param name="modelObjects">An object or array of objects which represent the data model</param>
        /// <returns>Fluent model action</returns>
		IFluentModelAction WithModel<T>(params T[] modelObjects) where T : class, new();
	}

	public interface IServiceLocator
	{
		IServiceProvider ServiceProvider { get; set; }
	}

	/// <summary>
	/// Represents a connection to a database
	/// </summary>
	public sealed class DataConnection : IDataConnection, IServiceLocator
	{
		// *************************************************
		// Properties
		// *************************************************

		/// <summary>
		/// Gets the underyling provider (e.g. Microsoft SQL Server) used to interact with the DBMS
		/// </summary>
		public IProvider Provider { get; private set; }

		/// <summary>
		/// Gets or sets a DI service provider to be used during data operations
		/// </summary>
		public IServiceProvider ServiceProvider { get; set; }

		// *************************************************
		// Constructor & Factory
		// *************************************************

		/// <summary>
		/// Creates a new database connection service using the given underlying provider
		/// </summary>
		/// <param name="provider">Provider to use. Consider using a convienence factory method, such as Blacksmiths.Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection()</param>
		public DataConnection(IProvider provider)
		{
			if (null == provider)
				throw new ArgumentNullException("provider may not be null");

			this.Provider = provider;
		}

		public static IDataConnection FromOptions(Utility.WolfConnectionOptions options)
		{
			var dc = options.NewDataConnection();
			if (null == dc)
				throw new InvalidOperationException("The provided options did not yield a data connection object");
			return dc;
		}

		// *************************************************
		// Methods
		// *************************************************

		/// <summary>
		/// Creates a new request for data
		/// </summary>
		/// <returns>A new data request</returns>
		public DataRequest NewRequest(Action<IDataRequestOptions> setOptions = null)
		{
			var ret = new DataRequest(this);
			if (null != setOptions)
				setOptions(ret);
			return ret;
		}

        /// <summary>
        /// Specifies a data model as a Wolf result model to perform an action with
        /// </summary>
        /// <param name="dataSet">A Wolf result model</param>
        /// <returns>Fluent model action</returns>
        public IFluentModelAction WithModel(Model.ResultModel model)
        {
            return this.WithModel<Model.ResultModel>(model);
        }

        /// <summary>
        /// Specifies a data model as ADO.NET DataTables to perform an action with
        /// </summary>
        /// <param name="dataSet">One or more ADO.NET DataTables which provide the model</param>
        /// <returns>Fluent model action</returns>
        public IFluentModelAction WithModel(DataTable[] dataTables)
        {
            return this.WithModel<DataTable>(dataTables);
        }

        /// <summary>
        /// Specifies a data model as an ADO.NET DataSet to perform an action with
        /// </summary>
        /// <param name="dataSet">An ADO.NET DataSet which provides the model</param>
        /// <returns>Fluent model action</returns>
        public IFluentModelAction WithModel(DataSet dataSet)
        {
            return this.WithModel<DataSet>(dataSet);
        }

        /// <summary>
        /// Specifies a data model as objects to perform an action with
        /// </summary>
        /// <typeparam name="T">Type of objects which represent the model</typeparam>
        /// <param name="modelObjects">An object or array of objects which represent the data model</param>
        /// <returns>Fluent model action</returns>
        public IFluentModelAction WithModel<T>(params T[] modelObjects) where T : class, new()
		{
			if (null == modelObjects)
				modelObjects = new T[0];

			modelObjects = modelObjects.Where(o => null != o).ToArray();

			if (modelObjects.Length > 0)
			{
				if (modelObjects[0] is Model.ResultModel model)
				{
					if (1 == modelObjects.Length)
						return new ModelProcessor(model, this);
					else
						throw new ArgumentException("When using a strongly typed model, only 1 strongly typed model must be supplied");
				}
				else if (modelObjects[0] is DataSet ds)
				{
					if (1 == modelObjects.Length)
						return new ModelProcessor(new Model.ResultModel(ds), this);
					else
						throw new ArgumentException("When using a DataSet as a model, only 1 DataSet must be supplied");
				}
				else if (modelObjects[0] is DataTable dt)
				{
					var dtds = new DataSet();
					foreach (var mdt in modelObjects.Cast<DataTable>())
						if (null == mdt.DataSet)
							dtds.Tables.Add(dt);
						else
							throw new ArgumentException($"DataTable '{dt.TableName}' belongs to a DataSet. To work with this model, pass the DataSet to .WithModel() rather than the DataTable");
					return new ModelProcessor(new Model.ResultModel(dtds), this);
				}
			}

			if(typeof(DataSet).IsAssignableFrom(typeof(T)) || typeof(DataTable).IsAssignableFrom(typeof(T)))
			{
				// ** Empty model returned for datasets
				return new ModelProcessor(new Model.ResultModel(), this);
			}
			else
			{
				// ** Loose objects. Create a model to represent them and then use an ad-hoc processor to switch to an overwrite persistance behaviour
				var SimpleModel = Model.ResultModel.CreateSimpleResultModel(modelObjects);
				return new AdHocModelProcessor(SimpleModel, this);
			}
		}

		// *************************************************
		// Engine room - Fetch Request
		// *************************************************

		internal DataResult Fetch(DataRequest request, DataResult Result = null)
		{
			if (null == request || 0 == request.Count)
				return null;

			Result = Result ?? new DataResult();
			Result.Request = request;

			// ** Connect to the database
			Utility.PerfDebuggers.BeginTrace("Request execution");

			using (var dbConnection = this.Provider.GetConnectionProvider().ToDbConnection())
			{
				System.Data.Common.DbTransaction dbTransaction = null;
				if (request.UseTransaction)
				{
					dbConnection.Open();
					dbTransaction = dbConnection.BeginTransaction();
				}

				var wolfWork = new List<Utility.WolfCommandBinding>(request.Select(ri => ri.GetDbCommand(this.Provider, dbConnection, dbTransaction)));
				Result.Commands = wolfWork.ToArray();

				try
				{

					// ** Process the commands
					foreach (var wolfCommand in wolfWork)
					{
						// ** Get a data adapter and fill a dataset
						var dbAdapter = this.Provider.GetDataAdapter(wolfCommand.DbCommand);
						dbAdapter.Fill(wolfCommand.ResultData);
						// ** The data adapter will now have executed the command. Perform binding back to the request objects
						wolfCommand.Bind();
					}

					dbTransaction?.Commit();
				}
				catch
				{
					dbTransaction?.Rollback();
					throw;
				}
			}

			Utility.PerfDebuggers.EndTrace("Request execution");

			return Result;
		}

        internal void FetchSchema(DataTable dt)
        {
            Utility.PerfDebuggers.BeginTrace($"Fetching PK information for '{Utility.QualifiedSqlName.From(dt).ToDisplayString()}'");

			//var wolfCommand = dt.ExtendedProperties[Utility.WolfCommandBinding.C_EXTENDED_WOLF_COMMAND] as Utility.WolfCommandBinding;
			//if(null != wolfCommand)
			//{
			//    using (var dbConnection = this.Provider.GetConnectionProvider().ToDbConnection())
			//    {
			//        wolfCommand.DbCommand.Connection = dbConnection;

			//        var dbAdapter = this.Provider.GetDataAdapter(wolfCommand.DbCommand); 
			//        dbAdapter.FillSchema(dt, SchemaType.Mapped);
			//    }
			//}

			using (var dbConnection = this.Provider.GetConnectionProvider().ToDbConnection())
			{
				var dbAdapter = this.Provider.GetDataAdapter(dt, dbConnection, null);
				var dbBuilder = this.Provider.GetCommandBuilder(dbAdapter);
				dbBuilder.GetInsertCommand();
				this.SyncSchemaInfo(dt, dbBuilder);
			}

			Utility.PerfDebuggers.EndTrace($"Fetching PK information for '{Utility.QualifiedSqlName.From(dt).ToDisplayString()}'");
        }

		// *************************************************
		// Engine room - Commit
		// *************************************************

		//private class CommitAction
		//{
		//	public System.Data.Common.DbDataAdapter adapter;
		//	public IEnumerable<DataRow> insertUpdateRows;
		//	public IEnumerable<DataRow> deleteRows;
		//}

		internal CommitResult Commit(ModelProcessor processor)
		{
			if (null == processor)
				throw new ArgumentNullException($"{nameof(processor)} may not be null");

			if(null != this.ServiceProvider)
			{
				var modelReviewer = this.ServiceProvider.GetService(typeof(IModelReviewer)) as IModelReviewer;
				if (null != modelReviewer)
					modelReviewer.ReviewModel(processor);
			}

			var ds = processor.ToDataSetForCommit();
			CommitResult ret = new CommitResult();

			if (null == ds)
				return ret;

			// ** Connect to the database
			using (var dbConnection = this.Provider.GetConnectionProvider().ToDbConnection())
			{
				dbConnection.Open();
				var dbTransaction = dbConnection.BeginTransaction();

				try
				{
					var orderedTables = this.OrderTablesForCommit(ds);
					var adapterDictionary = new Dictionary<DataTable, System.Data.Common.DbDataAdapter>();
					var rowsToCommit = new List<DataRow>(ds.Tables.Cast<DataTable>().Sum(dt => dt.Rows.Count));

					foreach (var table in orderedTables)
					{
						if (!Utility.DataTableHelpers.HasChanges(table))
							continue;

						var dbAdapter = this.Provider.GetDataAdapter(table, dbConnection, dbTransaction);
						var modelLinkCollection = Model.ModelLinkCollection.FromDataTable(table);
						var tableTarget = Utility.DataTableHelpers.GetTarget(table);

						if ((tableTarget?.CommitUsingSprocs).GetValueOrDefault())
						{
							if (null != tableTarget.InsertUsing)
								dbAdapter.InsertCommand = this.PrepCommand(tableTarget.InsertUsing, processor.ParameterValues, dbConnection, dbTransaction);
							if (null != tableTarget.UpdateUsing)
								dbAdapter.UpdateCommand = this.PrepCommand(tableTarget.UpdateUsing, processor.ParameterValues, dbConnection, dbTransaction);
							if (null != tableTarget.DeleteUsing)
								dbAdapter.DeleteCommand = this.PrepCommand(tableTarget.DeleteUsing, processor.ParameterValues, dbConnection, dbTransaction);
						}
						else
						{
							//TODO: DbCommandBuilder is paid for by an additional meta query to the DB. Probably can generate these commands by hand.
							var dbBuilder = processor.GetCommandBuilder(dbAdapter);
							dbAdapter.InsertCommand = this.PrepCommand(dbBuilder.GetInsertCommand(), dbTransaction);
							dbAdapter.UpdateCommand = this.PrepCommand(dbBuilder.GetUpdateCommand(), dbTransaction);
							dbAdapter.DeleteCommand = this.PrepCommand(dbBuilder.GetDeleteCommand(), dbTransaction);

							var syncFlags = this.SyncSchemaInfo(table, dbBuilder);

							if (syncFlags.HasFlag(SyncResultFlags.HasIdentity))
							{
								this.Provider.EnableIdentityColumnSyncing(dbAdapter, dbConnection, dbTransaction, Utility.DataTableHelpers.GetIdentityColumn(table).ColumnName, modelLinkCollection.ApplyIdentityValue);
							}
						}

						// ** Run processor actions
						processor.RaisePreCommitActions(table);

						// ** Prepare the DataAdapter
						this.CreateTableMappings(dbAdapter.TableMappings.Add(table.TableName, table.TableName), table);
						dbAdapter.MissingMappingAction = MissingMappingAction.Error;
						dbAdapter.MissingSchemaAction = MissingSchemaAction.Error;
						dbAdapter.ContinueUpdateOnError = false;

						// ** Prepare the DB change.
						//Utility.PerfDebuggers.BeginTrace("Row ordering");
						//var commitRows = this.OrderRowsForCommit(table).ToArray();
						//Utility.PerfDebuggers.EndTrace("Row ordering");

						//commitActions.Add(new CommitAction() { adapter = dbAdapter, insertUpdateRows = this.OrderRowsForInsert(table), deleteRows = this.OrderRowsForDeleteUpdate(table) });
						//ret.AffectedRowCount += dbAdapter.Update(commitRows);

						adapterDictionary.Add(table, dbAdapter);
						rowsToCommit.AddRange(this.GetRowsForCommit(table));
					}

					DataTable currentTable = null;
					System.Data.Common.DbDataAdapter currentAdapter = null;
					var batch = new List<DataRow>(rowsToCommit.Count);
					var debugRowsToCommit = rowsToCommit.OrderBy(r => r, new Utility.DataRowComparer());
					foreach (var row in debugRowsToCommit)
					{
						if(currentTable != row.Table)
						{
							if(batch.Count > 0)
							{
								ret.AffectedRowCount += currentAdapter.Update(batch.ToArray());
								batch.Clear();
							}
							currentTable = row.Table;
							currentAdapter = adapterDictionary[currentTable];
						}

						batch.Add(row);
					}

					if (batch.Count > 0)
					{
						ret.AffectedRowCount += currentAdapter.Update(batch.ToArray());
					}

					dbTransaction.Commit();
				}
				catch
				{
					dbTransaction.Rollback();
					throw;
				}
			}

			return ret;
		}

		private System.Data.Common.DbCommand PrepCommand(Type commandSprocType, IDictionary<string, object> parameters, System.Data.Common.DbConnection connection, System.Data.Common.DbTransaction trans)
		{
			if (!typeof(StoredProcedure).IsAssignableFrom(commandSprocType))
				throw new ArgumentException("When using InsertUsing, UpdateUsing or DeleteUsing, the type specified must be a StoredProcedure");
			var sproc = Activator.CreateInstance(commandSprocType) as StoredProcedure;
			var command = sproc.GetDbCommand(this.Provider, connection, trans);
			foreach (var param in command.Parameters)
				if (new[] { ParameterDirection.InputOutput, ParameterDirection.Input }.Contains(param.DbParameter.Direction)) //input params need binding to the source dt
				{
					if (!parameters.Keys.Contains(param.DbParameter.ParameterName))
						param.DbParameter.SourceColumn = param.DbParameter.ParameterName;
					else
						param.DbParameter.Value = parameters[param.DbParameter.ParameterName];
				}

			return command.DbCommand;
		}

		private System.Data.Common.DbCommand PrepCommand(System.Data.Common.DbCommand cmd, System.Data.Common.DbTransaction trans)
		{
			cmd.Transaction = trans;
			return cmd;
		}

		[Flags]
		private enum SyncResultFlags
		{
			None= 0,
			HasIdentity = 1,
		}

		private SyncResultFlags SyncSchemaInfo(DataTable table, System.Data.Common.DbCommandBuilder dbBuilder)
		{
			var ret = SyncResultFlags.None;
			if (null == table.PrimaryKey || 0 == table.PrimaryKey.Length)
			{
				// ** dbCommand builder knows about the table schema. If the source datatable doesn't know any PK info, sync it up so the adapter can work out how to do the commit
				var schemaDt = (DataTable)typeof(System.Data.Common.DbCommandBuilder).GetField("_dbSchemaTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dbBuilder);

				const string ColumnName = "ColumnName";
				const string IsKey = "IsKey";
				const string IsIdentity = "IsIdentity";
				const string IsAutoIncrement = "IsAutoIncrement";
				var PKcols = new List<DataColumn>();

				foreach (DataRow row in schemaDt.Rows)
				{
					var col = table.Columns[(string)row[ColumnName]];
					if ((bool)row[IsIdentity] && (bool)row[IsAutoIncrement])
					{
						Utility.DataTableHelpers.MarkIdentityColumn(col);
						ret |= SyncResultFlags.HasIdentity;

						col.AutoIncrement = true;
						col.AutoIncrementSeed = -1;
						col.AutoIncrementStep = -1;

						// ** Re-index existing rows
						long reindexer = 0;
						foreach(DataRow existingRow in table.Rows)
							if(existingRow.RowState == DataRowState.Added)
							{
								existingRow[col] = reindexer;
								reindexer++;
							}
					}

					if ((bool)row[IsKey])
						PKcols.Add(col);
				}

				table.PrimaryKey = PKcols.ToArray();
				//table.Constraints.Add(new UniqueConstraint(table.PrimaryKey));
			}
			else
			{
				if (table.Columns.Cast<DataColumn>().Any(c => c.AutoIncrement))
					ret |= SyncResultFlags.HasIdentity;

				var t = table.DataSet.Relations;
			}

			return ret;
		}

		private void CreateTableMappings(System.Data.Common.DataTableMapping mapping, DataTable table)
		{
			foreach (DataColumn column in table.Columns)
				mapping.ColumnMappings.Add(column.ColumnName, column.ColumnName);
		}

		

		private IEnumerable<DataTable> OrderTablesForCommit(DataSet ds)
		{
			return ds.Tables.Cast<DataTable>()
				.Where(dt => dt.Rows.Count > 0)
				.OrderBy(dt => dt, new Utility.DataTableComparer());
		}

		private IEnumerable<DataRow> GetRowsForCommit(DataTable dt)
		{
			return dt.Rows
				.Cast<DataRow>()
				.Where(dr => new[] { DataRowState.Added, DataRowState.Modified, DataRowState.Deleted }.Contains(dr.RowState));
		}

		private IEnumerable<DataRow> OrderRowsForCommit(DataTable dt)
		{
			return dt.Rows
				.Cast<DataRow>()
				.Where(dr => new[] { DataRowState.Added, DataRowState.Modified, DataRowState.Deleted }.Contains(dr.RowState))
				.OrderBy(dr => dr, new Utility.DataRowComparer());
		}


		private IEnumerable<DataRow> OrderRowsForInsert(DataTable dt)
		{
			return dt.Rows
				.Cast<DataRow>()
				.Where(dr => new[] { DataRowState.Added }.Contains(dr.RowState))
				.OrderBy(dr => dr, new Utility.DataRowComparer());
		}

		private IEnumerable<DataRow> OrderRowsForDeleteUpdate(DataTable dt)
		{
			return dt.Rows
				.Cast<DataRow>()
				.Where(dr => new[] { DataRowState.Deleted, DataRowState.Modified }.Contains(dr.RowState))
				.OrderBy(dr => dr, new Utility.DataRowComparer());
		}
	}
}
