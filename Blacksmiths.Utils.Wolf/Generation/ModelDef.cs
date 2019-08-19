/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Generation
{
	public sealed class ModelField
	{
		public string Name;
		public int Length;
		public bool AllowNulls;
		public string TypeName;
	}

	public sealed class ModelDef : System.Collections.ObjectModel.Collection<ModelField>
	{
		public string Name;
	}
}
