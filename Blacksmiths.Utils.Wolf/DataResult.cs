/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Blacksmiths.Utils.Wolf.Utility;
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace Blacksmiths.Utils.Wolf
{
	public interface IFluentResult
	{
		/// <summary>
		/// Creates a new DataSet with the results of the request as tables within it
		/// </summary>
		/// <returns></returns>
		DataSet ToDataSet();

		/// <summary>
		/// Merges the results of the request into the provided DataSet
		/// </summary>
		/// <param name="ds">DataSet to merge the results into</param>
		/// <returns></returns>
		DataSet ToDataSet(DataSet ds);

		/// <summary>
		/// Creates a new model object of the given type and populates it from the request data
		/// </summary>
		/// <typeparam name="T">Type of model to create</typeparam>
		/// <returns></returns>
		Model.SimpleResultModel<T> ToSimpleModel<T>() where T : class, new();

		/// <summary>
		/// Populates the given model object(s) from the request data
		/// </summary>
		/// <typeparam name="T">Type of the model</typeparam>
		/// <param name="model">Existing model collection objects</param>
		/// <returns></returns>
		Model.SimpleResultModel<T> ToSimpleModel<T>(params T[] model) where T : class, new();
	}

	/// <summary>
	/// Binds wolf command results to it's associated wolf request
	/// </summary>
	public class DataResult : IFluentResult
	{
		public WolfCommandBinding[] Commands { get; set; }
		public DataRequest Request { get; set; }

		public DataSet ToDataSet()
		{
			return this.ToDataSet(new DataSet());
		}

		public DataSet ToDataSet(DataSet ds)
		{
			if (null == ds)
				throw new ArgumentNullException("DataSet cannot be null");

			ds.EnforceConstraints = false;

			foreach (var cmd in this.Commands)
			{
				// ** Simple non-configurable merge for now.
				ds.Merge(cmd.ResultData);
			}

			ds.EnforceConstraints = true;

			return ds;
		}

		public Model.SimpleResultModel<T> ToSimpleModel<T>() where T : class, new()
		{
			return this.ToSimpleModel(new T[0]);
		}

		public Model.SimpleResultModel<T> ToSimpleModel<T>(params T[] model) where T : class, new()
		{
			// ** Sanity checks
			if (typeof(Model.ResultModel).IsAssignableFrom(typeof(T)))
				throw new ArgumentException("When using ToSimpleModel<T> T should be a plain object rather than another ResultModel");
			else if (typeof(DataSet).IsAssignableFrom(typeof(T)))
				throw new ArgumentException("DataSets should not be used with ToSimpleModel<T>, consider using ToDataSet()");

			var simpleModel = Model.ResultModel.CreateSimpleResultModel<T>(model);
			simpleModel.DataBind(this.ToDataSet());
			return simpleModel;
		}
	}

	public interface IFluentResultSp<T> : IFluentResult where T : StoredProcedure
	{
		T ToStoredProcedure();
	}

	public class DataResultSp<T> : DataResult, IFluentResultSp<T> where T : StoredProcedure
	{
		public T ToStoredProcedure()
		{
			if (1 == this.Commands.Length)
				return this.Commands[0].WolfRequestItem as T;
			else
				return null;
		}
	}
}
