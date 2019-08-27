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
		/// Merges the data model into an existing ADO.NET DataSet. Use this for strongly typed datasets produced using the Visual Studio DataSet designer.
		/// </summary>
		/// <param name="ds">Existing DataSet with results merged in</param>
		void MergeInto(DataSet ds);

		/// <summary>
		/// Commits the data model to the database store.
		/// </summary>
		/// <returns>Commit result information</returns>
		CommitResult Commit();
		IFluentModelAction AsUpdate();
		IFluentModelAction AsDelete();
	}

	public class CommitResult
	{
		public int AffectedRowCount { get; internal set; }
	}

	public class ModelProcessor : IFluentModelAction
	{
		public delegate void DataTableProcess(DataTable table);

		public Model.ResultModel Model { get; private set; }
		private DataConnection _connection;
		protected Func<DbDataAdapter, DbCommandBuilder> _GetCommandBuilder;

		protected List<DataTableProcess> PreCommitActions { get; } = new List<DataTableProcess>();

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
			var SourceDs = this.Model.GetDataSet();
			foreach (DataTable dt in SourceDs.Tables)
				this.RaisePreCommitActions(dt);
			return SourceDs;
		}

		public virtual void MergeInto(DataSet ds)
		{
			ds.Merge(this.GetModelDataSet());
		}

		internal virtual DataSet ToDataSetForCommit()
		{
			var ds = new DataSet();
			var SourceDs = this.Model.GetDataSet().GetChanges();
			if (null != SourceDs)
				ds.Merge(SourceDs);
			return ds;
		}

		public IFluentModelAction AsUpdate()
		{
			this._GetCommandBuilder = this.GetCommandBuilder_Overwriting;
			this.PreCommitActions.Add((table) =>
			{
				foreach (DataRow row in table.Rows)
					this.AdjustRowState(row, DataRowState.Modified);
			});
			return this;
		}

		public IFluentModelAction AsDelete()
		{
			this.PreCommitActions.Add((table) =>
			{
				foreach (DataRow row in table.Rows)
					this.AdjustRowState(row, DataRowState.Deleted);
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

		private void AdjustRowState(DataRow row, DataRowState requiredState)
		{
			if (row.RowState != requiredState)
			{
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
}
