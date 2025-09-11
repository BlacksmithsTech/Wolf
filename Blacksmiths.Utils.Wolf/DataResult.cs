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
using System.Data.Common;
using System.Linq;
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
			var nextCommand = this.GetNextUnconsumedCommandOrThrow();
            nextCommand.Reader ??= await this.EngineAsync(
				nextCommand,
				async (commandBinding) => await this.Connection.QueryMultipleAsync(commandBinding), 
				cancellationToken);

			var result = await nextCommand.Reader.ReadAsync<T>(buffered: false);
			return result;
		}

		public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
		{
			var nextCommand = this.GetNextUnconsumedCommandOrThrow();
            nextCommand.isConsumed = true;

            return await this.EngineAsync(
                nextCommand,
                async (commandBinding) => await this.Connection.ExecuteAsync(commandBinding),
				cancellationToken);
        }

		private WolfCommandBinding GetNextUnconsumedCommandOrThrow()
		{
			var result = this.GetNextUnconsumedCommand();
			if(null == result)
                throw new InvalidOperationException("There are no more commands remaining in this request to execute");
			return result;
        }

        private WolfCommandBinding GetNextUnconsumedCommand() 
			=> this.Commands.FirstOrDefault(c => !c.isConsumed);

        private CommandDefinition ToCommandDefinition(WolfCommandBinding commandBinding, CancellationToken cancellationToken)
			=> commandBinding.ToCommandDefinition(this.Provider, this.Connection, this.Transaction, (int?)this.Request.CommandTimeout?.TotalSeconds, cancellationToken);

		private async Task<T> EngineAsync<T>(WolfCommandBinding commandBinding, Func<CommandDefinition, Task<T>> operationAsync, CancellationToken cancellationToken, int minMs = 10, int maxMs = 100, int maxAttempts = 3)
		{
            minMs = Math.Max(0, minMs);
            maxMs = Math.Max(minMs, maxMs);

			var i = 0;
			while (true)
			{
				try
				{
					var commandDefinition = this.ToCommandDefinition(commandBinding, cancellationToken);
					var result = await operationAsync(commandDefinition);
					//TODO: Bind
					return result;
				}
				catch (DbException ex) when (this.Provider.isRetriedError(ex))
				{
					if (i >= maxAttempts)
						throw;

					var jitterMs = minMs == maxMs ? minMs : new Random().Next(minMs, maxMs + 1);

					object dbCommand = null;
					if (commandBinding.RequestItem.Command is StoredProcedure sp)
						dbCommand = new { ((IStoredProcedure)sp).ProcedureName, Parameters = sp.ToList() };	//Clone for logging reasons

					if (jitterMs > 0)
					{
						Utility.Logging.Log(Utility.LogLevel.Warning, $"A retryable database error was encountered {{@DbCommand}}. Retrying in {jitterMs}ms", dbCommand);
						await Task.Delay(jitterMs);
					}
					else
					{
						Utility.Logging.Log(Utility.LogLevel.Warning, $"A retryable database error was encountered {{@DbCommand}}. Retrying immediately", dbCommand);
					}
				}
				i++;
			}
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
