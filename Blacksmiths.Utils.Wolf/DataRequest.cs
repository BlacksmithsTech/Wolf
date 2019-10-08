﻿/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Blacksmiths.Utils.Wolf
{
	public sealed class DataRequest : Collection<IDataRequestItem>
	{
		// *************************************************
		// Fields
		// *************************************************


		// *************************************************
		// Properties
		// *************************************************

		public DataConnection Connection { get; private set; }

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
        /// Adds a new item (command) to the request. It is not executed until you call Execute()
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <returns></returns>
		public new DataRequest Add(IDataRequestItem item)
		{
			return this.Add(item, (string)null);
		}

        /// <summary>
        /// Adds a new item (command) to the request. It is not executed until you call Execute(). Specifies the name of a DataTable you would like the result data to be applied to.
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="Target">The name of the supplied DataTable will be used when applying the result data</param>
        /// <returns></returns>
        public DataRequest Add(IDataRequestItem item, System.Data.DataTable Target)
		{
			return this.Add(item, $"{Target.TableName}");
		}

        /// <summary>
        /// Adds a new item (command) to the request. It is not executed until you call Execute(). Specifies the name of the table you would like the result data to be applied to.
        /// </summary>
        /// <param name="item">Item to add</param>
        /// <param name="TargetTableName">The name of a table which will be used when applying the result data</param>
        /// <returns></returns>
		public DataRequest Add(IDataRequestItem item, string TargetTableName)
		{
			if (!string.IsNullOrEmpty(TargetTableName))
				item.TableName = TargetTableName;
			base.Add(item);
			return this;
		}

        /// <summary>
        /// Executes the items within this request against the database.
        /// </summary>
        /// <returns>Result commands</returns>
		public IFluentResult Execute()
		{
			return this.Connection.Fetch(this);
		}

		internal IFluentResult Execute(DataResult result)
		{
			return this.Connection.Fetch(this, result);
		}

		// *************************************************
		// Utility
		// *************************************************

		protected override void InsertItem(int index, IDataRequestItem item)
		{
			if (null == item)
				throw new ArgumentNullException("Cannot insert a null data request item");
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, IDataRequestItem item)
		{
			if (null == item)
				throw new ArgumentNullException("Cannot set a null data request item");
			base.SetItem(index, item);
		}
	}
}
