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

namespace Blacksmiths.Utils.Wolf
{
	public interface IFluentModelAction
	{
		DataSet ToDataSet();
		CommitResult Commit();
	}

	/// <summary>
	/// Represents a model that was created ad-hoc from objects with no change tracking
	/// </summary>
	public interface IFluentAdHocModelAction : IFluentModelAction
	{
		IFluentAdHocModelAction AsUpdate();
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
		protected List<DataTableProcess> PreCommitActions { get; } = new List<DataTableProcess>();

		public ModelProcessor(Model.ResultModel model, DataConnection connection)
		{
			this.Model = model;
			this._connection = connection;
		}

		public virtual CommitResult Commit()
		{
			return this._connection.Commit(this);
		}

		public virtual DataSet ToDataSet()
		{
			return this.Model.GetCopiedDataSet();
		}

		internal virtual DbCommandBuilder GetCommandBuilder(DbDataAdapter adapter)
		{
			return this._connection.Provider.GetCommandBuilder(adapter);
		}

		internal void RaisePreCommitActions(DataTable table)
		{
			foreach (var action in this.PreCommitActions)
				action(table);
		}
	}

	public class AdHocModelProcessor : ModelProcessor, IFluentAdHocModelAction
	{
		public AdHocModelProcessor(Model.ResultModel model, DataConnection connection)
			: base(model,connection) { }

		public IFluentAdHocModelAction AsUpdate()
		{
			this.PreCommitActions.Add((table) =>
			{
				foreach (DataRow row in table.Rows)
					this.AdjustRowState(row, DataRowState.Modified);
			});
			return this;
		}

		internal override DbCommandBuilder GetCommandBuilder(DbDataAdapter adapter)
		{
			// ** Ad hoc models have no tracking history. Commits won't use any concurrency checking and will simply overwrite database values using the PK.
			var cb = base.GetCommandBuilder(adapter);
			cb.ConflictOption = ConflictOption.OverwriteChanges;
			return cb;
		}

		private void AdjustRowState(DataRow row, DataRowState requiredState)
		{
			if(row.RowState != requiredState)
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

						foreach(DataColumn column in row.Table.Columns)
							if(!row.Table.PrimaryKey.Contains(column))
							{
								var ov = row[column];
								row[column] = DBNull.Value;
								row.AcceptChanges();
								row[column] = ov;
							}
						break;

					case DataRowState.Deleted:
						row.Delete();
						break;
				}
			}
		}
	}
}
