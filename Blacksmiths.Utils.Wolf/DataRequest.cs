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

        /// <summary>
        /// Executes the entire request and for convienience returns the return value of the last command in the collection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Return value of the last command in the collection</returns>
        Task<int> ExecuteAsync(CancellationToken cancellationToken = default);
    }

    public interface IDataRequestEmpty : IDataRequest
	{
        /// <summary>
        /// Adds a new item (command) to the request.
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <returns></returns>
        IDataRequestSingleCommand Add(IDataRequestCommand item);

		IDataRequestSingleCommand<TResult> Add<TResult>(IDataRequestCommand item);

        IDataRequestSingleCommand<TResult> Add<TResult>(IDataRequestCommand<TResult> item);
    }

    public interface IDataRequestSingleCommand : IDataRequest
	{
        /// <summary>
        /// Executes the single command as a query, buffering the results into a model of TResult
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<List<TResult>> QueryAsync<TResult>(CancellationToken cancellationToken = default);
    }

    public interface IDataRequestSingleCommand<TResult> : IDataRequestSingleCommand
	{
		/// <summary>
		/// Executes the single command as a query, buffering the results into a model of TResult
		/// </summary>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
        Task<List<TResult>> QueryAsync(CancellationToken cancellationToken = default);
    }

    public sealed class DataRequest : Collection<DataRequestItem>, IDataRequestEmpty, IDataRequestSingleCommand, IDataRequestOptions
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
		public IDataRequest Add(IDataRequestCommand item)
		{
			this.Add(new DataRequestItem() { Command = item });
			return this;
		}

		/// <summary>
		/// Adds a new command to the request with the specified model result type, indicating this command returns up to 1 result set of type TResult
		/// </summary>
		public DataRequest Add<TResult>(IDataRequestCommand item)
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

		IDataRequestSingleCommand IDataRequestEmpty.Add(IDataRequestCommand item) => (IDataRequestSingleCommand)this.Add(item);

		IDataRequestSingleCommand<TResult> IDataRequestEmpty.Add<TResult>(IDataRequestCommand item) => new DataRequestSingleCommand<TResult>(this.Add(item));
        IDataRequestSingleCommand<TResult> IDataRequestEmpty.Add<TResult>(IDataRequestCommand<TResult> item) => new DataRequestSingleCommand<TResult>(this.Add(item));

        async Task<List<TResult>> IDataRequestSingleCommand.QueryAsync<TResult>(CancellationToken cancellationToken)
		{
			using(var connection = this.OpenConnection())
				return await connection.QueryNextListAsync<TResult>(cancellationToken);
        }
    }

	public class DataRequestSingleCommand<TTargetedResult> : IDataRequestSingleCommand<TTargetedResult>
	{
		private readonly IDataRequest request;
		public DataRequestSingleCommand(IDataRequest request)
		{
			this.request = request;
		}

		public Task<int> ExecuteAsync(CancellationToken cancellationToken = default) => this.request.ExecuteAsync(cancellationToken);

		public DataResult OpenConnection() => this.request.OpenConnection();

		public Task<List<TResult>> QueryAsync<TResult>(CancellationToken cancellationToken = default) => ((IDataRequestSingleCommand)this.request).QueryAsync<TResult>(cancellationToken);

        public Task<List<TTargetedResult>> QueryAsync(CancellationToken cancellationToken = default) => ((IDataRequestSingleCommand)this.request).QueryAsync<TTargetedResult>(cancellationToken);
    }

	//public static class DataRequestExtensions
	//{
	//	/// <summary>
	//	/// Executes the entire request and for convienience returns the return value of the last command in the collection
	//	/// </summary>
	//	/// <param name="dataRequest"></param>
	//	/// <param name="cancellationToken"></param>
	//	/// <returns>Return value of the last command in the collection</returns>
 //       public static async Task<int> ExecuteAsync(this DataRequest dataRequest, CancellationToken cancellationToken = default)
 //       {
	//		int result = -1;
	//		using (var connection = dataRequest.OpenConnection())
	//			while (!connection.IsConsumed)
	//				result = await connection.ExecuteNextAsync(cancellationToken);
	//		return result;
 //       }

 // //      public static async Task<List<T>> QueryListAsync<T>(this DataRequest dataRequest, CancellationToken cancellationToken = default)
	//	//{
	//	//	using (var connection = dataRequest.OpenConnection())
	//	//		return await connection.QueryListAsync<T>(cancellationToken);
	//	//}

 //   }
}
