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
    /// <summary>
    /// Binds wolf command results to it's associated wolf request
    /// </summary>
    public class DataResult : IDisposable
	{
		private WolfCommandBinding _currentCommand;
		private int? _currentCommandResultIndex;

		internal IProvider Provider { get; set; }
		public bool IsConsumed => null == this.GetNextUnconsumedCommand();
		public DbTransaction Transaction { get; set; }
		public DbConnection Connection { get; set; }
		public WolfCommandBinding[] Commands { get; set; }
		public WolfCommandBinding CurrentCommand => this._currentCommand;
		public int? ResultIndex => this._currentCommandResultIndex;
		public DataRequest Request { get; set; }

        /// <summary>
        /// Opens the next grid of results and streams the row data into a type specified during request composition
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Row data of type T</returns>
        public async Task<IEnumerable<object>> StreamNextResultSetAsync(CancellationToken cancellationToken = default)
        {
            var nextCommand = this.MoveToNextUnconsumedCommandOrThrow();
			if (this.ResultIndex.Value >= (nextCommand.RequestItem.Types?.Length).GetValueOrDefault())
				throw new InvalidOperationException($"No result type specified for result set {this.ResultIndex.Value + 1:N0} of '{nextCommand}'");
			var resultType = nextCommand.RequestItem.Types[this.ResultIndex.Value];

            nextCommand.Reader ??= await this.EngineAsync(
                nextCommand,
                async (commandDefinition) => await this.Connection.QueryMultipleAsync(commandDefinition),
                cancellationToken);

            var result = await nextCommand.Reader.ReadAsync(resultType, buffered: false);
            return result;
        }

        /// <summary>
        /// Opens the next grid of results and streams the row data into the specified type.
        /// </summary>
		/// <param name="resultType">Type of data to stream</param>
        /// <param name="cancellationToken"></param>
        /// <returns>Row data of type T</returns>
        public async Task<IEnumerable<object>> StreamNextResultSetAsync(Type resultType, CancellationToken cancellationToken = default)
        {
            var nextCommand = this.MoveToNextUnconsumedCommandOrThrow();
            nextCommand.Reader ??= await this.EngineAsync(
                nextCommand,
                async (commandDefinition) => await this.Connection.QueryMultipleAsync(commandDefinition),
                cancellationToken);

            var result = await nextCommand.Reader.ReadAsync(resultType, buffered: false);
            return result;
        }

        /// <summary>
        /// Opens the next grid of results and streams the row data into T.
        /// </summary>
        /// <typeparam name="T">Type of data to stream</typeparam>
        /// <param name="cancellationToken"></param>
        /// <returns>Row data of type T</returns>
        public async Task<IEnumerable<T>> StreamNextResultSetAsync<T>(CancellationToken cancellationToken = default)
		{
			var nextCommand = this.MoveToNextUnconsumedCommandOrThrow();
            nextCommand.Reader ??= await this.EngineAsync(
				nextCommand,
				async (commandDefinition) => await this.Connection.QueryMultipleAsync(commandDefinition), 
				cancellationToken);

			try
			{
				return await nextCommand.Reader.ReadAsync<T>(buffered: false);
			}
			catch(InvalidOperationException ioe) when ("No columns were selected".Equals(ioe.Message))
			{
				// The sproc not returning may be intentional behaviour.
				return new List<T>();
			}
        }

		/// <summary>
		/// Binds the return value and output parameters to the last command which was executed in this result
		/// </summary>
		public void BindLastParameters()
		{
			this.Commands.Last(c => c.isConsumed)?.Bind();
		}

		/// <summary>
		/// Executes the next command in the request, binds to the command parameters, and returns only the return value, discarding any result sets
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns>Return value of the command</returns>
		public async Task<int> ExecuteNextAsync(CancellationToken cancellationToken = default)
		{
			var nextCommand = this.MoveToNextUnconsumedCommandOrThrow();
            nextCommand.isConsumed = true;

            await this.EngineAsync(
                nextCommand,
                async (commandDefinition) => await this.Connection.ExecuteAsync(commandDefinition),	//Dapper returns number of rows. Discarded.
				cancellationToken);

			nextCommand.Bind();

			if (nextCommand.RequestItem.Command is IStoredProcedure sp)
				return sp.ReturnValue.Value;
			else
				return -1;
        }

		private WolfCommandBinding MoveToNextUnconsumedCommandOrThrow()
		{
			var result = this.GetNextUnconsumedCommand();
			if(null == result)
                throw new InvalidOperationException("There are no more commands remaining in this request to execute");

			if(result != this._currentCommand)
			{
				this._currentCommand = result;
				this._currentCommandResultIndex = 0;
			}
			else
			{
				this._currentCommandResultIndex++;
			}

			return result;
        }

        private WolfCommandBinding GetNextUnconsumedCommand() 
			=> this.Commands.FirstOrDefault(c => !c.isConsumed);

   //     private CommandDefinition ToCommandDefinition(WolfCommandBinding commandBinding, CancellationToken cancellationToken)
			//=> commandBinding.ToCommandDefinition(this.Provider, this.Connection, this.Transaction, (int?)this.Request.CommandTimeout?.TotalSeconds, cancellationToken);

		private async Task<T> EngineAsync<T>(WolfCommandBinding commandBinding, Func<CommandDefinition, Task<T>> operationAsync, CancellationToken cancellationToken, int minMs = 10, int maxMs = 100, int maxAttempts = 3)
		{
            minMs = Math.Max(0, minMs);
            maxMs = Math.Max(minMs, maxMs);

			var i = 0;
			while (true)
			{
				try
				{
					//var commandDefinition = this.ToCommandDefinition(commandBinding, cancellationToken);
					var result = await operationAsync(commandBinding.SetCommandDefinition(this.Provider, this.Connection, this.Transaction, (int?)this.Request.CommandTimeout?.TotalSeconds, cancellationToken));
					//if (commandBinding.RequestItem.Command is IStoredProcedure sp)
					//	sp.SetDynamicParameters((DynamicParameters)commandDefinition.Parameters);
                    return result;
				}
				catch (DbException ex) when (this.Provider.isRetriedError(ex))
				{
					if (i >= maxAttempts)
					{
                        commandBinding.Exception = ex;
						throw;
                    }

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
                catch (Exception ex)
                {
                    commandBinding.Exception = ex;
					throw;
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

	public static class DataResultExtensions
	{
		public static async Task<List<T>> QueryNextListAsync<T>(this DataResult dataResult, CancellationToken cancellationToken = default)
		{
            List<T> result = [.. await dataResult.StreamNextResultSetAsync<T>(cancellationToken)];
			dataResult.BindLastParameters();
			return result;
        }
	}
}
