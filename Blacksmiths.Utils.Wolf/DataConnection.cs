/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;

namespace Blacksmiths.Utils.Wolf
{
	/// <summary>
	/// An ASP.NET Core scope capable database connection
	/// </summary>
	public sealed class DataConnection
	{
		// *************************************************
		// Properties
		// *************************************************

		/// <summary>
		/// Gets the underyling provider (e.g. Microsoft SQL Server) used to interact with the DBMS
		/// </summary>
		public IProvider Provider { get; private set; }

		// *************************************************
		// Constructor & Factory
		// *************************************************

		/// <summary>
		/// Creates a new database connection service using the given underlying provider
		/// </summary>
		/// <param name="provider">Provider to use. Consider using a convienence factory method, such as Blacksmiths.Utils.Wolf.SqlServer.SqlServerProvider.NewSqlServerConnection()</param>
		public DataConnection(IProvider provider)
		{
			if (null == provider)
				throw new ArgumentNullException("provider may not be null");

			this.Provider = provider;
		}

		// *************************************************
		// Methods
		// *************************************************

		/// <summary>
		/// Creates a new request for data
		/// </summary>
		/// <returns>A new data request</returns>
		public DataRequest NewRequest()
		{
			return new DataRequest(this);
		}

		// *************************************************
		// Engine room - Fetch Request
		// *************************************************

		internal DataResult Fetch(DataRequest request)
		{
			if (null == request || 0 == request.Count)
				return null;

			var Result = new DataResult();
			Result.Request = request;

			// ** Connect to the database
			using(var dbConnection = this.Provider.GetConnectionProvider().ToDbConnection())
			{
				var wolfWork = new List<Utility.WolfCommandBinding>(request.Select(ri => ri.GetDbCommand(this.Provider, dbConnection)));
				Result.Commands = wolfWork.ToArray();

				// ** Process the commands
				foreach(var wolfCommand in wolfWork)
				{
					// ** Get a data adapter and fill a dataset
					var dbAdapter = this.Provider.GetDataAdapter(wolfCommand.DbCommand);
					wolfCommand.ResultData = new DataSet();
					dbAdapter.Fill(wolfCommand.ResultData);

					// ** The data adapter will now have executed the command. Perform binding back to the request objects
					wolfCommand.Bind();
				}
			}

			return Result;
		}
	}
}
