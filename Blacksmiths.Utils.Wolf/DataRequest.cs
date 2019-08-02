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

		public new DataRequest Add(IDataRequestItem item)
		{
			base.Add(item);
			return this;
		}

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
