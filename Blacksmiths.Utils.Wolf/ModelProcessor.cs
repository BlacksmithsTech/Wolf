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

namespace Blacksmiths.Utils.Wolf
{
	public interface IFluentModelAction
	{
		DataSet ToDataSet();
		CommitResult Commit();
	}

	public class CommitResult
	{
		public int AffectedRowCount { get; internal set; }
	}

	public class ModelProcessor : IFluentModelAction
	{
		private Model.ResultModel _model;
		private DataConnection _connection;

		public ModelProcessor(Model.ResultModel model, DataConnection connection)
		{
			this._model = model;
			this._connection = connection;
		}

		public CommitResult Commit()
		{
			return this._connection.Commit(this._model);
		}

		public DataSet ToDataSet()
		{
			return this._model.GetCopiedDataSet();
		}
	}
}
