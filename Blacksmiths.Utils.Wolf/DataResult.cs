/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Blacksmiths.Utils.Wolf.Utility;
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Blacksmiths.Utils.Wolf
{
	public interface IFluentResult : IDisposable
	{
		Task<IEnumerable<T>> StreamNextResultSetAsync<T>(CancellationToken cancellationToken = default);
    }

	///// <summary>
	///// Asyncronous helpers for cleaner consumer code
	///// </summary>
	//public static class DataResultExtensions
	//{
	//	public static async Task<DataSet> ToDataSetAsync(this Task<IFluentResult> result) => (await result).ToDataSet();

 //       public static async Task<Model.SimpleResultModel<T>> ToSimpleModelAsync<T>(this Task<IFluentResult> result) where T : class, new() => (await result).ToSimpleModel<T>();

	//	public static async Task<T> ToModelAsync<T>(this Task<IFluentResult> result) where T : Model.ResultModel, new() => (await result).ToModel<T>();

 //       public static async Task<List<T>> ToListAsync<T>(this Task<IFluentResult> result) where T : class, new() => (await result).ToSimpleModel<T>().Results;

	//	public static async Task<T> ToFirstAsync<T>(this Task<IFluentResult> result) where T : class, new() => (await result).ToSimpleModel<T>().Results.First();

	//	public static async Task<T> ToFirstOrDefaultAsync<T>(this Task<IFluentResult> result) where T : class, new() => (await result).ToSimpleModel<T>().Results.FirstOrDefault();
 //   }

    /// <summary>
    /// Binds wolf command results to it's associated wolf request
    /// </summary>
    public class DataResult : IFluentResult, IDisposable
	{
		internal IProvider Provider { get; set; }
		public DbTransaction Transaction { get; set; }
		public DbConnection Connection { get; set; }
		public WolfCommandBinding[] Commands { get; set; }
		public DataRequest Request { get; set; }

		public async Task<IEnumerable<T>> StreamNextResultSetAsync<T>(CancellationToken cancellationToken = default)
		{
			var nextCommand = this.Commands.FirstOrDefault(c => null == c.Reader || !c.Reader.IsConsumed);
			//TODO: Resiliance
			nextCommand.Reader ??= await this.Connection.QueryMultipleAsync(nextCommand.ToCommandDefinition(this.Provider, this.Connection, this.Transaction, (int?)this.Request.CommandTimeout?.TotalSeconds, cancellationToken));
			return await nextCommand.Reader.ReadAsync<T>(buffered: false);
		}

		public void Dispose()
        {
			foreach (var command in this.Commands)
				command.Dispose();
            this.Connection?.Dispose();
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
				return this.Commands[0].RequestItem.Command as T;
			else
				return null;
		}
	}
}
