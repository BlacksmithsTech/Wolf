/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Utility
{
	public interface IDataConnectionFactory
	{
		IDataConnection NewDataConnection(WolfConnectionOptions options);
	}

	public sealed class WolfConnectionOptions : Dictionary<string, string>
	{
		public WolfConnectionOptions()
		: base(StringComparer.CurrentCultureIgnoreCase) { }

		private const string Key_Provider = "Provider";

		public string Provider
		{
			get { return this.GetValue(Key_Provider) ?? "Blacksmiths.Utils.Wolf.Utility.SqlServerProviderFactory, Blacksmiths.Utils.Wolf.SqlServer"; }
			set { this[Key_Provider] = value; }
		}

		public IDataConnection NewDataConnection()
		{
			var FactoryType = Type.GetType(this.Provider);
			if (null == FactoryType)
				throw new ArgumentException($"The provider factory type '{this.Provider}' couldn't be located. Check the type name is correct and any required assemblies can be located (are you missing a dependency?)");
			var Factory = Activator.CreateInstance(FactoryType) as IDataConnectionFactory;
			if (null == Factory)
				throw new ArgumentException($"The provider factory type '{this.Provider}' must implement Blacksmiths.Utils.Wolf.Utility.IDataConnectionFactory");

			return Factory.NewDataConnection(this);
		}

		public string GetValue(string Key)
		{
			if (this.ContainsKey(Key))
				return this[Key];
			else
				return null;
		}
	}
}