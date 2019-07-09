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
	/// <summary>
	/// An ASP.NET Core scope capable collection of connections
	/// </summary>
	public sealed class DataConnectionCollection : Collection<DataConnection>
	{
		protected override void InsertItem(int index, DataConnection item)
		{
			if (null == item)
				throw new ArgumentNullException("item cannot be null");

			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, DataConnection item)
		{
			if (null == item)
				throw new ArgumentNullException("item cannot be null");

			base.SetItem(index, item);
		}
	}
}
