/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;

namespace Blacksmiths.Utils.Wolf
{
	public interface IDataConnection
	{
        /// <summary>
        /// Creates a new data request object, which you can use to batch up a number of database commands.
        /// </summary>
        /// <returns></returns>
		DataRequest NewRequest(Action<IDataRequestOptions> setOptions = null);
	}

	public interface IServiceLocator
	{
		IServiceProvider ServiceProvider { get; set; }
	}

	/// <summary>
	/// Represents a connection to a database
	/// </summary>
	public sealed class DataConnection : IDataConnection, IServiceLocator
	{
		// *************************************************
		// Properties
		// *************************************************

		/// <summary>
		/// Gets the underyling provider (e.g. Microsoft SQL Server) used to interact with the DBMS
		/// </summary>
		public IProvider Provider { get; private set; }

		/// <summary>
		/// Gets or sets a DI service provider to be used during data operations
		/// </summary>
		public IServiceProvider ServiceProvider { get; set; }

		// *************************************************
		// Constructor & Factory
		// *************************************************

		/// <summary>
		/// Creates a new database connection service using the given underlying provider
		/// </summary>
		/// <param name="provider">Provider to use. Consider using a convienence factory method, such as Blacksmiths.Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection()</param>
		public DataConnection(IProvider provider, IServiceProvider serviceProvider = null)
		{
			if (null == provider)
				throw new ArgumentNullException("provider may not be null");

			this.Provider = provider;
			this.ServiceProvider = serviceProvider;
		}

		public static IDataConnection FromOptions(Utility.WolfConnectionOptions options)
		{
			var dc = options.NewDataConnection();
			if (null == dc)
				throw new InvalidOperationException("The provided options did not yield a data connection object");
			return dc;
		}

		// *************************************************
		// Methods
		// *************************************************

		/// <summary>
		/// Creates a new request for data
		/// </summary>
		/// <returns>A new data request</returns>
		public DataRequest NewRequest(Action<IDataRequestOptions> setOptions = null)
		{
			var ret = new DataRequest(this);
			if (null != setOptions)
				setOptions(ret);
			return ret;
		}

        // *************************************************
        // Engine room - Fetch Request
        // *************************************************

        internal DataResult OpenConnection(DataRequest request, DataResult result = null)
		{
			if (null == request || 0 == request.Count)
				return null;

			result ??= new DataResult();
            result.Provider = this.Provider;
            result.Request = request;
            result.Connection = this.Provider.GetConnectionProvider().ToDbConnection();

            if (request.UseTransaction)
            {
                result.Connection.Open();
                result.Transaction = result.Connection.BeginTransaction();
            }

			result.Commands = request.Select(ri => ri.ToCommandBinding()).ToArray();

			return result;
		}
	}
}
