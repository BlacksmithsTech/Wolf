/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Blacksmiths.Utils.Wolf.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Blacksmiths.Utils.Wolf
{
	public interface IDataRequestOptions
	{
		/// <summary>
		/// Gets or sets if the data request will execute all queries within the scope of a database transaction. Defaults to false.
		/// </summary>
		bool UseTransaction { get; set; }

		/// <summary>
		/// Gets or sets the maximum number of attempts to run the request work. Defaults to 3.
		/// </summary>
		short? MaximumAttempts { get; set; }

		/// <summary>
		/// Gets or sets the minimum time in ms before a data request is retried, to introduce jittering. Defaults to 10ms
		/// </summary>
		short? RetryJitterMinimumMs { get; set; }

		/// <summary>
		/// Gets or sets the maximum time in ms before data request is retried, to introduce jittering. Defaults to 100ms
		/// </summary>
		short? RetryJitterMaximumMs { get; set; }

		/// <summary>
		/// The timespan, to the nearest second, before a single command execution times out
		/// </summary>
		TimeSpan? CommandTimeout { get; set; }
	}

	public sealed class DataRequestItem
	{
		/// <summary>
		/// The model type(s) of the result data
		/// </summary>
		internal Type[] Types { get; set; }

		/// <summary>
		/// The database command driving the result
		/// </summary>
		internal IDataRequestCommand Command { get; set; }

		internal WolfCommandBinding ToCommandBinding() => new WolfCommandBinding()
		{
			RequestItem = this,
		};

        public override string ToString()
        {
			if (this.Command is IStoredProcedure sp)
				return sp.ProcedureName;
			else
				return base.ToString();
        }
	}

	//public interface IDataRequestFluentExecutable
	//{
	//	/// <summary>
	//	/// Executes the entire request and for convienience returns the return value of the last command in the collection
	//	/// </summary>
	//	/// <param name="cancellationToken"></param>
	//	/// <returns>Return value of the last command in the collection</returns>
	//	Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
 //   }

	//public interface IDataRequestFluentSingleCommand : IDataRequestFluentExecutable
	//{
	//	Task<List<T>> QueryListAsync<T>(CancellationToken cancellationToken = default);

 //   }

	public interface IDataRequest
	{
        /// <summary>
        /// Advanced. Opens a database connection and transaction (if requested), where the consumer is in control of the disposal.
        /// </summary>
        DataResult OpenConnection();

        ///// <summary>
        ///// Executes the entire request and maps the results into the provided TGraph
        ///// </summary>
        ///// <typeparam name="TGraph">Type of graph</typeparam>
        ///// <param name="instance">An instance to map the results into</param>
        ///// <param name="cancellationToken"></param>
        ///// <returns></returns>
        //Task<TGraph> MapAsync<TGraph>(TGraph instance, CancellationToken cancellationToken = default);
    }

	public interface IDataRequestCommandCollection : IDataRequest
	{
		IDataRequestCommandCollection Add(IDataRequestCommand item);
		IDataRequestCommandCollection Add<TResult>(IDataRequestCommand item);
    }

    public interface IDataRequestExecutable : IDataRequest
	{
        /// <summary>
        /// Executes the entire request and for convienience returns the return value of the last command in the collection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Return value of the last command in the collection</returns>
        Task<int> ExecuteAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the entire request and maps the results into a collection of TGraph
        /// </summary>
        /// <typeparam name="TGraph">Object graph type to be mapped from source data</typeparam>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<TGraph>> MapAsync<TGraph>(CancellationToken cancellationToken = default);
    }

    public interface IDataRequestEmpty : IDataRequest
	{
  //      /// <summary>
  //      /// Adds a new item (command) to the request.
  //      /// </summary>
  //      /// <param name="item">Item to add</param>
  //      /// <returns></returns>
  //      IDataRequestSingleCommand Add(IDataRequestCommand item);

		//IDataRequestSingleCommand<TResult> Add<TResult>(IDataRequestCommand item);

  //      IDataRequestSingleCommand<TResult> Add<TResult>(IDataRequestCommand<TResult> item);
    }

	public interface IDataRequestSingle : IDataRequestExecutable { }
	public interface IDataRequestMulti : IDataRequestExecutable { }
	public interface IDataRequestExecutable<TResult> : IDataRequestExecutable { }
	public interface IDataRequestSingle<TResult> : IDataRequestExecutable<TResult> { }
	public interface IDataRequestMulti<TResult> : IDataRequestExecutable<TResult> { }

 //   public interface IDataRequestMultiCommand : IDataRequest
 //   {
 //       /// <summary>
 //       /// Adds a new item (command) to the request.
 //       /// </summary>
 //       /// <param name="item">Item to add</param>
 //       /// <returns></returns>
 //       IDataRequestMultiCommand Add(IDataRequestCommand item);

 //       IDataRequestMultiCommand Add<TResult>(IDataRequestCommand item);

 //       IDataRequestMultiCommand Add<TResult>(IDataRequestCommand<TResult> item);
 //   }

	//public interface IDataRequestMultiCommand<TResult> : IDataRequestMultiCommand
	//{
 //       /// <summary>
 //       /// Executes the entire request and maps the results into a collection type defined by the command
 //       /// </summary>
 //       /// <param name="cancellationToken"></param>
 //       /// <returns></returns>
 //       Task<List<TResult>> MapAsync(CancellationToken cancellationToken = default);
 //   }

 //   public interface IDataRequestSingleCommand : IDataRequest, IDataRequestMultiCommand
	//{
 //       /// <summary>
 //       /// Executes the single command as a query, buffering the results into a model of TResult
 //       /// </summary>
 //       /// <typeparam name="TResult"></typeparam>
 //       /// <param name="cancellationToken"></param>
 //       /// <returns></returns>
 //       Task<List<TResult>> QueryAsync<TResult>(CancellationToken cancellationToken = default);
 //   }

 //   public interface IDataRequestSingleCommand<TResult> : IDataRequestSingleCommand, IDataRequestMultiCommand<TResult>
 //   {
	//	/// <summary>
	//	/// Executes the single command as a query, buffering the results into a model defined by the command
	//	/// </summary>
	//	/// <param name="cancellationToken"></param>
	//	/// <returns></returns>
 //       Task<List<TResult>> QueryAsync(CancellationToken cancellationToken = default);
 //   }

    public sealed class DataRequest : Collection<DataRequestItem>, IDataRequestOptions, IDataRequestCommandCollection, IDataRequestEmpty, IDataRequestSingle, IDataRequestMulti
	{
		// *************************************************
		// Fields
		// *************************************************

		// *************************************************
		// Properties
		// *************************************************

		public DataConnection Connection { get; private set; }

		public bool UseTransaction { get; set; }

		public short? MaximumAttempts { get; set; } = 3;

		public short? RetryJitterMinimumMs { get; set; } = 10;

		public short? RetryJitterMaximumMs { get; set; } = 100;

		public TimeSpan? CommandTimeout { get; set; }

		// *************************************************
		// Constructor
		// *************************************************

		public DataRequest(DataConnection connection)
		{
			if (null == connection)
				throw new ArgumentNullException("connection may not be null");
			this.Connection = connection;
		}

		// *************************************************
		// Methods
		// *************************************************

		/// <summary>
		/// Specifies that this request will run all commands within a transaction
		/// </summary>
		public DataRequest WithTransaction(bool useTransaction = true)
		{
			this.UseTransaction = useTransaction;
			return this;
		}

		/// <summary>
		/// Specifies that this request will run all commands with a given timeout timespan in milliseconds
		/// </summary>
		public DataRequest WithCommandTimeout(int milliseconds) => this.WithCommandTimeout(TimeSpan.FromMilliseconds(milliseconds));

		/// <summary>
		/// Specifies that this request will run all commands with a given timeout timespan
		/// </summary>
		public DataRequest WithCommandTimeout(TimeSpan? commandTimeout = null)
		{
			this.CommandTimeout = commandTimeout;
			return this;
		}

		/// <summary>
		/// Adds a new item (command) to the request.
		/// </summary>
		/// <param name="item">Item to add</param>
		/// <returns></returns>
		public IDataRequestCommandCollection Add(IDataRequestCommand item)
		{
			this.Add(new DataRequestItem() { Command = item });
			return this;
		}

		/// <summary>
		/// Adds a new command to the request with the specified model result type, indicating this command returns up to 1 result set of type TResult
		/// </summary>
		public IDataRequestCommandCollection Add<TResult>(IDataRequestCommand item)
		{
			this.Add(new DataRequestItem() { Command = item, Types = [typeof(TResult)] });
			return this;
		}

		/// <summary>
		/// Advanced. Opens a database connection and transaction (if requested), where the consumer is in control of the disposal.
		/// </summary>
		public DataResult OpenConnection()
		{
			return this.Connection.OpenConnection(this);
		}

		internal DataResult OpenConnection(DataResult result)
		{
			return this.Connection.OpenConnection(this, result);
		}

		/// <summary>
		/// Executes the entire request and for convienience returns the return value of the last command in the collection
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns>Return value of the last command in the collection</returns>
		public async Task<int> ExecuteAsync(CancellationToken cancellationToken = default)
        {
            int result = -1;
            using (var connection = this.OpenConnection())
                while (!connection.IsConsumed)
                    result = await connection.ExecuteNextAsync(cancellationToken);
            return result;
        }

		public async Task<List<TGraph>> MapAsync<TGraph>(CancellationToken cancellationToken = default)
			=> Utility.GraphMapper.MapList<TGraph>(await this.BufferAsync(cancellationToken));

        //public async Task<TGraph> MapAsync<TGraph>(TGraph instance, CancellationToken cancellationToken = default)
        //    => Utility.GraphMapper.MapList(instance, await this.BufferAsync(cancellationToken));

        private async Task<Dictionary<Type, List<object>>> BufferAsync(CancellationToken cancellationToken = default)
		{
			var result = new Dictionary<Type, List<object>>();
			using var connection = this.OpenConnection();
			
			while (!connection.IsConsumed)
			{
				var rows = await connection.StreamNextResultSetAsync(cancellationToken);
				var rowType = connection.CurrentCommand.RequestItem.Types[connection.ResultIndex.Value];
				if (!result.ContainsKey(rowType))
					result.Add(rowType, [.. rows]);
				else
					result[rowType].AddRange(rows);
			}

			return result;
        }

  //      IDataRequestSingleCommand IDataRequestEmpty.Add(IDataRequestCommand item) => (IDataRequestSingleCommand)this.Add(item);

		//IDataRequestSingleCommand<TResult> IDataRequestEmpty.Add<TResult>(IDataRequestCommand item) => new DataRequestSingleCommand<TResult>(this.Add(item));
  //      IDataRequestSingleCommand<TResult> IDataRequestEmpty.Add<TResult>(IDataRequestCommand<TResult> item) => new DataRequestSingleCommand<TResult>(this.Add<TResult>(item));

  //      async Task<List<TResult>> IDataRequestSingleCommand.QueryAsync<TResult>(CancellationToken cancellationToken)
		//{
		//	using(var connection = this.OpenConnection())
		//		return await connection.QueryNextListAsync<TResult>(cancellationToken);
  //      }

  //      IDataRequestMultiCommand IDataRequestMultiCommand.Add(IDataRequestCommand item) => (IDataRequestMultiCommand)this.Add(item);
    }

	//public class DataRequestSingleCommand<TTargetedResult> : IDataRequestSingleCommand<TTargetedResult>
	//{
	//	private readonly IDataRequest request;
	//	public DataRequestSingleCommand(IDataRequest request)
	//	{
	//		this.request = request;
	//	}

	//	public Task<int> ExecuteAsync(CancellationToken cancellationToken = default) => this.request.ExecuteAsync(cancellationToken);

	//	public Task<List<TGraph>> MapAsync<TGraph>(CancellationToken cancellationToken = default) => this.request.MapAsync<TGraph>(cancellationToken);

	//	public Task<List<TTargetedResult>> MapAsync(CancellationToken cancellationToken = default) => this.request.MapAsync<TTargetedResult>(cancellationToken);

	//	//public Task<TGraph> MapAsync<TGraph>(TGraph instance, CancellationToken cancellationToken = default) => this.request.MapAsync(instance, cancellationToken);

 //       public DataResult OpenConnection() => this.request.OpenConnection();

	//	public Task<List<TResult>> QueryAsync<TResult>(CancellationToken cancellationToken = default) => ((IDataRequestSingleCommand)this.request).QueryAsync<TResult>(cancellationToken);

 //       public Task<List<TTargetedResult>> QueryAsync(CancellationToken cancellationToken = default) => ((IDataRequestSingleCommand)this.request).QueryAsync<TTargetedResult>(cancellationToken);

	//	IDataRequestMultiCommand IDataRequestMultiCommand.Add(IDataRequestCommand item) => ((IDataRequestSingleCommand)this.request).Add(item);
 //   }

	public sealed class DataRequestTargeted<TTargetedResult> : IDataRequestCommandCollection, IDataRequestSingle, IDataRequestMulti, IDataRequestSingle<TTargetedResult>, IDataRequestMulti<TTargetedResult>
	{
		private readonly IDataRequestCommandCollection request;
		public DataRequestTargeted(IDataRequestCommandCollection request)
		{
			this.request = request;
		}

		public DataResult OpenConnection() => this.request.OpenConnection();
		public IDataRequestCommandCollection Add(IDataRequestCommand item)
		{
			this.request.Add(item);
			return this;
		}
		public IDataRequestCommandCollection Add<TResult>(IDataRequestCommand item)
		{
            this.request.Add<TResult>(item);
			return this;
        }

		public Task<int> ExecuteAsync(CancellationToken cancellationToken = default) => ((IDataRequestExecutable)this.request).ExecuteAsync(cancellationToken);
		public Task<List<TGraph>> MapAsync<TGraph>(CancellationToken cancellationToken = default) => ((IDataRequestExecutable)this.request).MapAsync<TGraph>(cancellationToken);
    }

    public static class DataRequestExtensions
	{
		// Add - From Empty

		public static IDataRequestSingle<TResult> Add<TResult>(this IDataRequestEmpty dataRequest, IDataRequestCommand command) => new DataRequestTargeted<TResult>(((IDataRequestCommandCollection)dataRequest).Add<TResult>(command));

		public static IDataRequestSingle<TDefaultResult> Add<TDefaultResult>(this IDataRequestEmpty dataRequest, IDataRequestCommand<TDefaultResult> command)
			=> Add<TDefaultResult>(dataRequest, (IDataRequestCommand)command);

        public static IDataRequestSingle Add(this IDataRequestEmpty dataRequest, IDataRequestCommand command) => (IDataRequestSingle)((IDataRequestCommandCollection)dataRequest).Add(command);

        // Add - From Single, Untargeted

        public static IDataRequestMulti<TResult> Add<TResult>(this IDataRequestSingle dataRequest, IDataRequestCommand command) => new DataRequestTargeted<TResult>(((IDataRequestCommandCollection)dataRequest).Add<TResult>(command));

        public static IDataRequestMulti<TDefaultResult> Add<TDefaultResult>(this IDataRequestSingle dataRequest, IDataRequestCommand<TDefaultResult> command)
            => Add<TDefaultResult>(dataRequest, (IDataRequestCommand)command);

        public static IDataRequestMulti Add(this IDataRequestSingle dataRequest, IDataRequestCommand command) => (IDataRequestMulti)((IDataRequestCommandCollection)dataRequest).Add(command);

        // Add - From Single, Targeted

        public static IDataRequestMulti<TResult> Add<TResult, TDefaultResult>(this IDataRequestSingle<TResult> dataRequest, IDataRequestCommand<TDefaultResult> command) => (IDataRequestMulti<TResult>)((IDataRequestCommandCollection)dataRequest).Add<TDefaultResult>(command);

        public static IDataRequestMulti<TResult> Add<TResult>(this IDataRequestSingle<TResult> dataRequest, IDataRequestCommand command) => (IDataRequestMulti<TResult>)((IDataRequestCommandCollection)dataRequest).Add(command);

        // Add - From Multi, Untargeted

        public static IDataRequestMulti<TResult> Add<TResult>(this IDataRequestMulti dataRequest, IDataRequestCommand command) => new DataRequestTargeted<TResult>(((IDataRequestCommandCollection)dataRequest).Add<TResult>(command));

        public static IDataRequestMulti<TDefaultResult> Add<TDefaultResult>(this IDataRequestMulti dataRequest, IDataRequestCommand<TDefaultResult> command)
            => Add<TDefaultResult>(dataRequest, (IDataRequestCommand)command);

        // Add - From Multi, Targeted

        public static IDataRequestMulti<TResult> Add<TResult, TDefaultResult>(this IDataRequestMulti<TResult> dataRequest, IDataRequestCommand<TDefaultResult> command) => (IDataRequestMulti<TResult>)((IDataRequestCommandCollection)dataRequest).Add<TDefaultResult>(command);
        public static IDataRequestMulti<TResult> Add<TResult>(this IDataRequestMulti<TResult> dataRequest, IDataRequestCommand command) => (IDataRequestMulti<TResult>)((IDataRequestCommandCollection)dataRequest).Add(command);

        // Query - Type explicitly specified

        public static async Task<List<TResult>> QueryAsync<TResult>(this IDataRequestSingle dataRequest, CancellationToken cancellationToken = default)
		{
			using (var connection = dataRequest.OpenConnection())
				return await connection.QueryNextListAsync<TResult>(cancellationToken);
		}

		public static async Task<TResult> QueryFirstAsync<TResult>(this IDataRequestSingle dataRequest, CancellationToken cancellationToken = default)
		{
			using (var connection = dataRequest.OpenConnection())
				return (await connection.StreamNextResultSetAsync<TResult>(cancellationToken)).First();
		}

        public static async Task<TResult> QueryFirstOrDefaultAsync<TResult>(this IDataRequestSingle dataRequest, CancellationToken cancellationToken = default)
        {
            using (var connection = dataRequest.OpenConnection())
                return (await connection.StreamNextResultSetAsync<TResult>(cancellationToken)).FirstOrDefault();
        }

        public static async Task<TResult> QuerySingleAsync<TResult>(this IDataRequestSingle dataRequest, CancellationToken cancellationToken = default)
        {
			using (var connection = dataRequest.OpenConnection())
				return (await connection.StreamNextResultSetAsync<TResult>(cancellationToken)).Single();
        }

        public static async Task<TResult> QuerySingleOrDefaultAsync<TResult>(this IDataRequestSingle dataRequest, CancellationToken cancellationToken = default)
        {
            using (var connection = dataRequest.OpenConnection())
                return (await connection.StreamNextResultSetAsync<TResult>(cancellationToken)).SingleOrDefault();
        }

		// Query - Type implicitly specified

		public static Task<List<TResult>> QueryAsync<TResult>(this IDataRequestSingle<TResult> dataRequest, CancellationToken cancellationToken = default)
			=> QueryAsync <TResult>((IDataRequestSingle)dataRequest, cancellationToken);
        public static Task<TResult> QueryFirstAsync<TResult>(this IDataRequestSingle<TResult> dataRequest, CancellationToken cancellationToken = default)
            => QueryFirstAsync<TResult>((IDataRequestSingle)dataRequest, cancellationToken);
        public static Task<TResult> QueryFirstOrDefaultAsync<TResult>(this IDataRequestSingle<TResult> dataRequest, CancellationToken cancellationToken = default)
            => QueryFirstOrDefaultAsync<TResult>((IDataRequestSingle)dataRequest, cancellationToken);
        public static Task<TResult> QuerySingleAsync<TResult>(this IDataRequestSingle<TResult> dataRequest, CancellationToken cancellationToken = default)
            => QuerySingleAsync<TResult>((IDataRequestSingle)dataRequest, cancellationToken);
        public static Task<TResult> QuerySingleOrDefaultAsync<TResult>(this IDataRequestSingle<TResult> dataRequest, CancellationToken cancellationToken = default)
            => QuerySingleOrDefaultAsync<TResult>((IDataRequestSingle)dataRequest, cancellationToken);

		// Map - Type explicitly specified

        public static async Task<TResult> MapFirstAsync<TResult>(this IDataRequestExecutable dataRequest, CancellationToken cancellationToken = default)
			=> (await dataRequest.MapAsync<TResult>(cancellationToken)).First();
		public static async Task<TResult> MapFirstOrDefaultAsync<TResult>(this IDataRequestExecutable dataRequest, CancellationToken cancellationToken = default)
			=> (await dataRequest.MapAsync<TResult>(cancellationToken)).FirstOrDefault();
        public static async Task<TResult> MapSingleAsync<TResult>(this IDataRequestExecutable dataRequest, CancellationToken cancellationToken = default)
            => (await dataRequest.MapAsync<TResult>(cancellationToken)).Single();
        public static async Task<TResult> MapSingleOrDefaultAsync<TResult>(this IDataRequestExecutable dataRequest, CancellationToken cancellationToken = default)
            => (await dataRequest.MapAsync<TResult>(cancellationToken)).SingleOrDefault();

		// Map - Type implicitly specified
		public static Task<List<TResult>> MapAsync<TResult>(this IDataRequestExecutable<TResult> dataRequest, CancellationToken cancellationToken = default)
			=> dataRequest.MapAsync<TResult>(cancellationToken);
        public static Task<TResult> MapFirstAsync<TResult>(this IDataRequestExecutable<TResult> dataRequest, CancellationToken cancellationToken = default)
			=> MapFirstAsync<TResult>((IDataRequestExecutable)dataRequest, cancellationToken);
        public static Task<TResult> MapFirstOrDefaultAsync<TResult>(this IDataRequestExecutable<TResult> dataRequest, CancellationToken cancellationToken = default)
            => MapFirstOrDefaultAsync<TResult>((IDataRequestExecutable)dataRequest, cancellationToken);
        public static Task<TResult> MapSingleAsync<TResult>(this IDataRequestExecutable<TResult> dataRequest, CancellationToken cancellationToken = default)
            => MapSingleAsync<TResult>((IDataRequestExecutable)dataRequest, cancellationToken);
        public static Task<TResult> MapSingleOrDefaultAsync<TResult>(this IDataRequestExecutable<TResult> dataRequest, CancellationToken cancellationToken = default)
            => MapSingleOrDefaultAsync<TResult>((IDataRequestExecutable)dataRequest, cancellationToken);
    }
}
