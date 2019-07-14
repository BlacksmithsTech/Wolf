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
	/// <summary>
	/// An ASP.NET Core scope capable database connection
	/// </summary>
	public sealed class DataConnection
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

		public IFluentModelAction WithModel(Model.ResultModel resultModel)
		{
			if(null == resultModel)
				throw new ArgumentNullException($"{nameof(resultModel)} cannot be null");
			return new ModelProcessor(resultModel, this);
		}

		public IFluentAdHocModelAction WithModel<T>(params T[] modelObjects) where T : class
		{
			if (null == modelObjects)
				throw new ArgumentNullException($"{nameof(modelObjects)} cannot be null");
			if (0 == modelObjects.Length)
				throw new ArgumentException($"{nameof(modelObjects)} cannot be of empty length");
			if (typeof(T) == typeof(Model.ResultModel))
				throw new ArgumentException("Model.ResultModel can't be used with WithModel<T>");

			var SimpleModel = Model.ResultModel.CreateSimpleResultModel(modelObjects);
			return new AdHocModelProcessor(SimpleModel, this);
		}

		// *************************************************
		// Engine room - Fetch Request
		// *************************************************

		internal DataResult Fetch(DataRequest request)
		{
			if (null == request || 0 == request.Count)
				return null;

			var Result = new DataResult();
			Result.Request = request;

			// ** Connect to the database
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

			return Result;
		}

		// *************************************************
		// Engine room - Commit
		// *************************************************

		internal CommitResult Commit(ModelProcessor processor)
		{
			if (null == processor)
				throw new ArgumentNullException($"{nameof(processor)} may not be null");

			var ds = processor.ToDataSet().GetChanges();
			CommitResult ret = new CommitResult();

			//TODO: optimise for when there's nothing to do

			// ** Connect to the database
			using (var dbConnection = this.Provider.GetConnectionProvider().ToDbConnection())
			{
				dbConnection.Open();
				var dbTransaction = dbConnection.BeginTransaction();

				try
				{
					foreach(var table in this.OrderTablesForCommit(ds))
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

				foreach(DataRow row in schemaDt.Rows)
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

		private IEnumerable<DataTable> OrderTablesForCommit(DataSet ds)
		{
			return ds.Tables.Cast<DataTable>();
		}
	}
}
