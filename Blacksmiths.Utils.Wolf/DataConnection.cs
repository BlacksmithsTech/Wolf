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
		DataRequest NewRequest();
		//IFluentModelAction WithModel(Model.ResultModel model);
		IFluentModelAction WithModel<T>(params T[] modelObjects) where T : class, new();
	}

	/// <summary>
	/// Represents a connection to a database
	/// </summary>
	public sealed class DataConnection : IDataConnection
	{
		// *************************************************
		// Properties
		// *************************************************

		/// <summary>
		/// Gets the underyling provider (e.g. Microsoft SQL Server) used to interact with the DBMS
		/// </summary>
		public IProvider Provider { get; private set; }

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
		public DataRequest NewRequest()
		{
			return new DataRequest(this);
		}

		public IFluentModelAction WithModel<T>(params T[] modelObjects) where T : class, new()
		{
			if (null == modelObjects)
				modelObjects = new T[0];

			modelObjects = modelObjects.Where(o => null != o).ToArray();

			if (typeof(Model.ResultModel).IsAssignableFrom(typeof(T)))
			{
				if (1 == modelObjects.Length && modelObjects[0] is Model.ResultModel model)
					return new ModelProcessor(model, this);
				else if (modelObjects.Length > 1)
					throw new ArgumentException("When using a strongly typed model, only 1 strongly typed model must be supplied");
				else
					return new ModelProcessor(new Model.ResultModel(), this);
			}
			else if (typeof(DataSet).IsAssignableFrom(typeof(T)))
			{
				if (1 == modelObjects.Length && modelObjects[0] is DataSet ds)
					return new ModelProcessor(new Model.ResultModel(ds), this);
				else if (modelObjects.Length > 1)
					throw new ArgumentException("When using a DataSet as a model, only 1 DataSet must be supplied");
				else
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
				var wolfWork = new List<Utility.WolfCommandBinding>(request.Select(ri => ri.GetDbCommand(this.Provider, dbConnection)));
				Result.Commands = wolfWork.ToArray();

				// ** Process the commands
				foreach (var wolfCommand in wolfWork)
				{
					// ** Get a data adapter and fill a dataset
					var dbAdapter = this.Provider.GetDataAdapter(wolfCommand.DbCommand);
					wolfCommand.ResultData = new DataSet();
					dbAdapter.Fill(wolfCommand.ResultData);

					// ** The data adapter will now have executed the command. Perform binding back to the request objects
					wolfCommand.Bind();
				}
			}

			Utility.PerfDebuggers.EndTrace("Request execution");

			return Result;
		}

		// *************************************************
		// Engine room - Commit
		// *************************************************

		internal CommitResult Commit(ModelProcessor processor)
		{
			if (null == processor)
				throw new ArgumentNullException($"{nameof(processor)} may not be null");

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
					foreach (var table in this.OrderTablesForCommit(ds))
					{
						var dbAdapter = this.Provider.GetDataAdapter(table, dbConnection, dbTransaction);

						//TODO: DbCommandBuilder is paid for by an additional meta query to the DB. Probably can generate these commands by hand.
						var dbBuilder = processor.GetCommandBuilder(dbAdapter);
						dbAdapter.InsertCommand = this.PrepCommand(dbBuilder.GetInsertCommand(), dbTransaction);
						dbAdapter.UpdateCommand = this.PrepCommand(dbBuilder.GetUpdateCommand(), dbTransaction);
						dbAdapter.DeleteCommand = this.PrepCommand(dbBuilder.GetDeleteCommand(), dbTransaction);

						this.SyncSchemaInfo(table, dbBuilder);

						// ** Run processor actions
						processor.RaisePreCommitActions(table);

						// ** Prepare the DataAdapter
						this.CreateTableMappings(dbAdapter.TableMappings.Add(table.TableName, table.TableName), table);
						dbAdapter.MissingMappingAction = MissingMappingAction.Error;
						dbAdapter.MissingSchemaAction = MissingSchemaAction.Error;
						dbAdapter.ContinueUpdateOnError = false;

						// ** Perform the DB change
						ret.AffectedRowCount += dbAdapter.Update(table);
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

		private System.Data.Common.DbCommand PrepCommand(System.Data.Common.DbCommand cmd, System.Data.Common.DbTransaction trans)
		{
			cmd.Transaction = trans;
			return cmd;
		}

		private void SyncSchemaInfo(DataTable table, System.Data.Common.DbCommandBuilder dbBuilder)
		{
			if (null == table.PrimaryKey || 0 == table.PrimaryKey.Length)
			{
				// ** dbCommand builder knows about the table schema. If the source datatable doesn't know any PK info, sync it up so the adapter can work out how to do the commit
				var schemaDt = (DataTable)typeof(System.Data.Common.DbCommandBuilder).GetField("_dbSchemaTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(dbBuilder);

				const string ColumnName = "ColumnName";
				const string IsKey = "IsKey";
				var PKcols = new List<DataColumn>();

				foreach (DataRow row in schemaDt.Rows)
				{
					var col = table.Columns[(string)row[ColumnName]];
					if ((bool)row[IsKey])
						PKcols.Add(col);
				}

				table.PrimaryKey = PKcols.ToArray();
			}
		}

		private void CreateTableMappings(System.Data.Common.DataTableMapping mapping, DataTable table)
		{
			foreach (DataColumn column in table.Columns)
				mapping.ColumnMappings.Add(column.ColumnName, column.ColumnName);
		}

		private sealed class DataTableComparer : IComparer<DataTable>
		{
			public int Compare(DataTable x, DataTable y)
			{
				if (this.IsChildOfY(x, y))
					return 1;
				else if (this.IsParentOfY(x, y))
					return -1;
				return 0;
			}

			private bool IsChildOfY(DataTable x, DataTable y)
			{
				foreach (var relation in x.ParentRelations.Cast<DataRelation>())
				{
					if (y == relation.ParentTable)
						return true;

					var ParentComparison = this.IsChildOfY(relation.ParentTable, y);
					if (ParentComparison)
						return ParentComparison;
				}

				return false;
			}

			private bool IsParentOfY(DataTable x, DataTable y)
			{
				foreach (var relation in x.ChildRelations.Cast<DataRelation>())
				{
					if (y == relation.ChildTable)
						return true;

					var ChildComparison = this.IsParentOfY(relation.ChildTable, y);
					if (ChildComparison)
						return ChildComparison;
				}

				return false;
			}
		}

		private IEnumerable<DataTable> OrderTablesForCommit(DataSet ds)
		{
			return ds.Tables.Cast<DataTable>()
				.Where(dt => dt.Rows.Count > 0)
				.OrderBy(dt => dt, new DataTableComparer());
		}
	}
}
