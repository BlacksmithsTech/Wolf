/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Blacksmiths.Utils.Wolf
{
	public interface IDataConnection
	{
        /// <summary>
        /// Creates a new data request object, which you can use to batch up a number of database commands.
        /// </summary>
        /// <returns></returns>
		DataRequest NewRequest(Action<IDataRequestOptions> setOptions = null);
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
		public DataConnection(IProvider provider, IServiceProvider serviceProvider = null)
		{
			if (null == provider)
				throw new ArgumentNullException("provider may not be null");

			this.Provider = provider;
			this.ServiceProvider = serviceProvider;
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

        // *************************************************
        // Engine room - Fetch Request
        // *************************************************

        internal DataResult OpenConnection(DataRequest request, DataResult result = null)
		{
			if (null == request || 0 == request.Count)
				return null;

			result ??= new DataResult();
            result.Provider = this.Provider;
            result.Request = request;
            result.Connection = this.Provider.GetConnectionProvider().ToDbConnection();

            if (request.UseTransaction)
            {
                result.Connection.Open();
                result.Transaction = result.Connection.BeginTransaction();
            }

			result.Commands = request.Select(ri => ri.ToCommandBinding()).ToArray();

   //         short batchAttempt = 0;
			//short maxBatchAttempts = 1;
			//short maxCommandAttempts = 1;
			//if(request.UseTransaction)
			//	maxBatchAttempts = Math.Max((short)1, request.MaximumAttempts.GetValueOrDefault(1));
			//else
			//	maxCommandAttempts = Math.Max((short)1, request.MaximumAttempts.GetValueOrDefault(1));

   //         Exceptions.RequestExecutionException batchRetryException = null;
			//string batchTraceLabel;

			//// Execution loop (resiliance)
			//do
			//{
   //             if (null != batchRetryException)
   //                 this.Jitter(batchRetryException, request.RetryJitterMinimumMs, request.RetryJitterMaximumMs);
   //             batchRetryException = null;
   //             batchAttempt++;
			//	batchTraceLabel = $"Batch execution attempt {batchAttempt} of {maxBatchAttempts}";

   //             // ** Connect to the database
   //             Utility.Logging.BeginTrace(batchTraceLabel);

			//	using (var dbConnection = this.Provider.GetConnectionProvider().ToDbConnection())
			//	{
			//		System.Data.Common.DbTransaction dbTransaction = null;
			//		if (request.UseTransaction)
			//		{
			//			dbConnection.Open();
			//			dbTransaction = dbConnection.BeginTransaction();
			//		}

			//		var wolfWork = new List<Utility.WolfCommandBinding>(request.Select(ri => ri.ToCommandBinding(this.Provider, dbConnection, dbTransaction, (int?)request.CommandTimeout?.TotalSeconds, cancellationToken)));
			//		result.Commands = wolfWork.ToArray();
			//		Utility.WolfCommandBinding lastCommandBinding = null;

			//		try
			//		{
			//			// ** Process the commands
			//			foreach (var wolfCommand in wolfWork)
			//			{
			//				lastCommandBinding = wolfCommand;
			//				short commandAttempt = 0;
   //                         Exceptions.RequestExecutionException commandRetryException = null;
   //                         string commandTraceLabel;

   //                         do
   //                         {
   //                             if (null != commandRetryException)
   //                                 this.Jitter(commandRetryException, request.RetryJitterMinimumMs, request.RetryJitterMaximumMs);
   //                             commandRetryException = null;
   //                             commandAttempt++;
   //                             commandTraceLabel = $"{wolfCommand.CommandDefinition.CommandText} execution attempt {commandAttempt} of {maxCommandAttempts}";
   //                             Utility.Logging.BeginTrace(commandTraceLabel);

   //                             try
   //                             {
			//						// ** Get a data adapter and fill a dataset
			//						wolfCommand.Reader = await dbConnection.QueryMultipleAsync(wolfCommand.CommandDefinition);
			//					}
			//					catch (Exception ex)
			//					{
			//						if (commandAttempt < maxCommandAttempts && this.Provider.isRetriedError(ex as DbException))
			//							commandRetryException = new Exceptions.RequestExecutionException(request, ex) { CommandBinding = lastCommandBinding };
			//						else if(!cancellationToken.IsCancellationRequested)//SqlClient provides no good exception type or error code for the exception which is thrown during cancellation. Simply assume if we're here this exception was due to cancellation.
			//							throw;
			//					}
			//					finally
			//					{
   //                                 Utility.Logging.EndTrace(commandTraceLabel);
   //                             }

   //                         } while (null != commandRetryException);

			//				if (cancellationToken.IsCancellationRequested)	// cancelled after the command execution completed
			//					break;
   //                     }

			//			dbTransaction?.Commit();
			//		}
			//		catch (Exception ex)
			//		{
			//			try
			//			{
			//				dbTransaction?.Rollback();
			//			}
			//			catch(Exception rollbackEx)
			//			{
			//				Utility.Logging.Log(Utility.LogLevel.Information, "A transaction rollback threw an exception {rollbackEx}. Usually this means the transaction was already rolled back.", rollbackEx);
			//			}

   //                     if (batchAttempt < maxBatchAttempts && this.Provider.isRetriedError(ex as DbException))
   //                         batchRetryException = new Exceptions.RequestExecutionException(request, ex) { CommandBinding = lastCommandBinding };
   //                     else
   //                         throw new Exceptions.RequestExecutionException(request, ex) { CommandBinding = lastCommandBinding };
   //                 }
   //             }

			//	Utility.Logging.EndTrace(batchTraceLabel);
			//} while (null != batchRetryException);

			//cancellationToken.ThrowIfCancellationRequested();
			return result;
		}

		//private void Jitter(Exceptions.RequestExecutionException ex, int? minMs, int? maxMs)
		//{
		//	minMs = Math.Max(0, minMs.GetValueOrDefault(0));
		//	maxMs = Math.Max(minMs.Value, maxMs.GetValueOrDefault(0));
  //          var jitterMs = minMs.Value == maxMs.Value ? minMs.Value : new Random().Next(minMs.Value, maxMs.Value + 1);

  //          var wri = ex.CommandBinding?.RequestCommand;
		//	object dbCommand = null;
  //          if (wri is StoredProcedure sp)
  //              dbCommand = new { ((IStoredProcedure)sp).ProcedureName, Parameters = sp.ToList() };

  //          if (jitterMs > 0)
		//	{
		//		Utility.Logging.Log(Utility.LogLevel.Warning, $"A retryable database error was encountered {{@DbCommand}}. Retrying in {jitterMs}ms", dbCommand);
		//		System.Threading.Thread.Sleep(jitterMs);
		//	}
		//	else
		//	{
  //              Utility.Logging.Log(Utility.LogLevel.Warning, $"A retryable database error was encountered {{@DbCommand}}. Retrying immediately", dbCommand);
  //          }
  //      }

        internal void FetchSchema(DataTable dt)
        {
            Utility.Logging.BeginTrace($"Fetching PK information for '{Utility.QualifiedSqlName.From(dt).ToDisplayString()}'");

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

			Utility.Logging.EndTrace($"Fetching PK information for '{Utility.QualifiedSqlName.From(dt).ToDisplayString()}'");
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

		//private IEnumerable<DataRow> OrderRowsForCommit(DataTable dt)
		//{
		//	return dt.Rows
		//		.Cast<DataRow>()
		//		.Where(dr => new[] { DataRowState.Added, DataRowState.Modified, DataRowState.Deleted }.Contains(dr.RowState))
		//		.OrderBy(dr => dr, new Utility.DataRowComparer());
		//}


		//private IEnumerable<DataRow> OrderRowsForInsert(DataTable dt)
		//{
		//	return dt.Rows
		//		.Cast<DataRow>()
		//		.Where(dr => new[] { DataRowState.Added }.Contains(dr.RowState))
		//		.OrderBy(dr => dr, new Utility.DataRowComparer());
		//}

		//private IEnumerable<DataRow> OrderRowsForDeleteUpdate(DataTable dt)
		//{
		//	return dt.Rows
		//		.Cast<DataRow>()
		//		.Where(dr => new[] { DataRowState.Deleted, DataRowState.Modified }.Contains(dr.RowState))
		//		.OrderBy(dr => dr, new Utility.DataRowComparer());
		//}
	}
}
