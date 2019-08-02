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
		DataSet ToDataSet();
		DataSet ToDataSet(DataSet ds);
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
