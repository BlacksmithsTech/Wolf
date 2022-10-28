/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Linq;
using Blacksmiths.Utils.Wolf.Model;

namespace Blacksmiths.Utils.Wolf
{
	public interface IFluentModelAction : IFluentResult
	{
		/// <summary>
		/// Commits the data model to the database store.
		/// </summary>
		/// <returns>Commit result information</returns>
		CommitResult Commit();
		/// <summary>
		/// Shortcut for AsCommand(new[] { DataRowState.Added }, mode)
		/// </summary>
		/// <returns></returns>
		IFluentModelAction AsInsert(ModelProcessor.ModelUpdateMode mode);
		/// <summary>
		/// Shortcut for AsCommand(new[] { DataRowState.Modified }, mode)
		/// </summary>
		/// <returns></returns>
		IFluentModelAction AsUpdate(ModelProcessor.ModelUpdateMode mode);
		/// <summary>
		/// Shortcut for AsCommand(new[] { DataRowState.Deleted }, mode)
		/// </summary>
		/// <returns></returns>
		IFluentModelAction AsDelete(ModelProcessor.ModelUpdateMode mode);
		/// <summary>
		/// Allows the model persistance commands to be ignored or overriden.
		/// </summary>
		/// <param name="commands">What persistance commands are acceptable</param>
		/// <param name="mode">How changes should be applied</param>
		/// <returns></returns>
		IFluentModelAction AsCommand(DataRowState[] commands, ModelProcessor.ModelUpdateMode mode);
		/// <summary>
		/// Provides fixed parameter values into stored procedures that are being used for CUD operations
		/// </summary>
		IFluentModelAction WithParameter(string parameterName, object value);
	}

	public class CommitResult
	{
		public int AffectedRowCount { get; internal set; }
	}

	public class ModelProcessor : IFluentModelAction
	{
		public enum ModelUpdateMode
        {
			/// <summary>
			/// Forces all data in the model to be applied as this change operation
			/// </summary>
			ForceCommand,

			/// <summary>
			/// Other command operations will be rejected
			/// </summary>
			RejectOtherCommands,

			/// <summary>
			/// Attempt to run on an other command will throw an exception
			/// </summary>
			ThrowOtherCommands,
        }

		public delegate void DataTableProcess(DataTable table);

		public Model.ResultModel Model { get; private set; }
		private DataConnection _connection;
		protected Func<DbDataAdapter, DbCommandBuilder> _GetCommandBuilder;

		protected List<DataTableProcess> PreCommitActions { get; } = new List<DataTableProcess>();

		public Dictionary<string, object> ParameterValues { get; } = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);

		public ModelProcessor(Model.ResultModel model, DataConnection connection)
		{
			this.Model = model;
			this._connection = connection;
			this._GetCommandBuilder = this.GetCommandBuilder_Default;
		}

		public virtual CommitResult Commit()
		{
			var Result = this._connection.Commit(this);
			this.Model.AcceptChanges();
			return Result;
		}

		protected virtual DataSet GetModelDataSet()
		{
			var SourceDs = this.Model.GetDataSet(this._connection);
			foreach (DataTable dt in SourceDs.Tables)
				this.RaisePreCommitActions(dt);
			return SourceDs;
		}

		internal virtual DataSet ToDataSetForCommit()
		{
			return this.Model.GetDataSet(this._connection);
		}

		public IFluentModelAction AsInsert(ModelUpdateMode mode) => this.AsCommand(new[] { DataRowState.Added }, mode);

		public IFluentModelAction AsUpdate(ModelUpdateMode mode) => this.AsCommand(new[] { DataRowState.Modified }, mode);

		public IFluentModelAction AsDelete(ModelUpdateMode mode) => this.AsCommand(new[] { DataRowState.Deleted }, mode);

		public IFluentModelAction AsCommand(DataRowState[] commands, ModelUpdateMode mode)
		{
			if (null == commands)
				throw new ArgumentNullException(nameof(commands));
			if (mode == ModelUpdateMode.ForceCommand && commands.Length > 1)
				throw new ArgumentException("When forcing a command, only one command may be specified");
			if (commands.Contains(DataRowState.Modified))
				this._GetCommandBuilder = this.GetCommandBuilder_Overwriting;

			this.PreCommitActions.Add((table) =>
			{
				foreach (DataRow row in table.Rows)
					this.AdjustRowState(row, commands, mode);
			});
			return this;
		}

		internal virtual DbCommandBuilder GetCommandBuilder(DbDataAdapter adapter)
		{
			return this._GetCommandBuilder(adapter);
		}

		protected virtual DbCommandBuilder GetCommandBuilder_Default(DbDataAdapter adapter)
		{
			return this._connection.Provider.GetCommandBuilder(adapter);
		}

		protected virtual DbCommandBuilder GetCommandBuilder_Overwriting(DbDataAdapter adapter)
		{
			var cb = this.GetCommandBuilder_Default(adapter);
			cb.ConflictOption = ConflictOption.OverwriteChanges;
			return cb;
		}

		internal void RaisePreCommitActions(DataTable table)
		{
			foreach (var action in this.PreCommitActions)
				action(table);
		}

		private void AdjustRowState(DataRow row, DataRowState[] requiredStates, ModelUpdateMode mode)
		{
			if (!requiredStates.Contains(row.RowState))
			{
				if (mode == ModelUpdateMode.ForceCommand)
				{
					var requiredState = requiredStates[0];

					switch (requiredState)
					{
						case DataRowState.Added:
							row.SetAdded();
							break;

						case DataRowState.Modified:
							if (null == row.Table.PrimaryKey || 0 == row.Table.PrimaryKey.Length)
								throw new InvalidOperationException("To force the changes as updates, a primary key is required.");

							if (row.RowState == DataRowState.Deleted)
								row.RejectChanges();
							else if (row.RowState == DataRowState.Added)
								row.AcceptChanges();

							foreach (DataColumn column in row.Table.Columns)
								if (!row.Table.PrimaryKey.Contains(column))
								{
									var ov = row[column];
									row[column] = DBNull.Value;
									row.AcceptChanges();
									row[column] = ov;
									break;
								}
							break;

						case DataRowState.Deleted:
							if (row.RowState == DataRowState.Added)
								row.AcceptChanges();
							row.Delete();
							break;
					}
				}
				else if(row.RowState != DataRowState.Unchanged)
                {
					if (mode == ModelUpdateMode.RejectOtherCommands)
					{
						row.RejectChanges();
					}
					else if (mode == ModelUpdateMode.ThrowOtherCommands)
					{
						throw new Exceptions.ModelProcessorException(this.Model, $"A row was {row.RowState} but the commit only allows {String.Join(", ", requiredStates)}") { AffectedRow = row };
					}
				}
			}
		}

        public virtual DataSet ToDataSet()
        {
            return this.ToDataSet<DataSet>();
        }

        public T ToDataSet<T>() where T : DataSet, new()
        {
            var ds = this.GetModelDataSet();
            if (ds is T)
                return (T)ds;
            else
                return this.ToDataSet<T>((T)ds);
        }

        public T ToDataSet<T>(T ds) where T : DataSet
        {
            if (this.Model.IsSameDs(ds))
                return (T)ds;
            ds.Merge(this.GetModelDataSet());
            return ds;
        }

        public SimpleResultModel<T> ToSimpleModel<T>() where T : class, new()
        {
            return this.ToSimpleModel<T>(null);
        }

        public SimpleResultModel<T> ToSimpleModel<T>(params T[] model) where T : class, new()
        {
            return DataResult.ToSimpleModel<T>(this.ToDataSet(), model);
        }

		public T ToModel<T>() where T : ResultModel, new()
		{
			return DataResult.ToModel<T>(this.ToDataSet());
		}

		public IFluentModelAction WithParameter(string parameterName, object value)
		{
			if (!this.ParameterValues.ContainsKey(parameterName))
				this.ParameterValues.Add(parameterName, value);
			else
				this.ParameterValues[parameterName] = value;

			return this;
		}
	}

	public class AdHocModelProcessor : ModelProcessor
	{
		public AdHocModelProcessor(Model.ResultModel model, DataConnection connection)
			: base(model,connection)
		{
			// ** Ad hoc models have no tracking history. Commits won't use any concurrency checking and will simply overwrite database values using the PK.
			this._GetCommandBuilder = this.GetCommandBuilder_Overwriting;
		}
	}

	public interface IModelReviewer
	{
		void ReviewModel(IFluentModelAction model);
	}
}
