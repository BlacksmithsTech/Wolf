/*
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
	public interface IDataRequestOptions
	{
		/// <summary>
		/// Gets or sets if the data request will execute all queries within the scope of a database transaction. Defaults to false.
		/// </summary>
		bool UseTransaction { get; set; }
	}

	public sealed class DataRequest : Collection<IDataRequestItem>, IDataRequestOptions
	{
		// *************************************************
		// Fields
		// *************************************************


		// *************************************************
		// Properties
		// *************************************************

		public DataConnection Connection { get; private set; }

		public bool UseTransaction { get; set; }

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
			var fqName = Utility.QualifiedSqlName.Parse(Target.TableName);
			if (!Target.TableName.Equals(fqName.Name))
				Target.TableName = fqName.Name;
			if (!Target.Namespace.Equals(fqName.Schema))
				Target.Namespace = fqName.Schema;
			return this.Add(item, fqName.ToString());
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
				item.TableName = Utility.QualifiedSqlName.Parse(TargetTableName);
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
