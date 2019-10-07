/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Text;
using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.Utility;
using System.Linq;

#if NETSTANDARD
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddWolfSqlServer(this IServiceCollection services, Action<WolfOptionsSqlServer> options = null)
		{
			services.AddScoped<IDataConnection>((provider) =>
			{
				var sqlOptions = new WolfOptionsSqlServer(provider.GetRequiredService<IConfiguration>());
				if (null != options)
					options(sqlOptions);

				sqlOptions.AutoConfigureFromConfiguration();

				return DataConnection.FromOptions(sqlOptions.Options);
			});

			return services;
		}
	}
}
#endif

namespace Blacksmiths.Utils.Wolf.Utility
{
	public sealed class SqlServerProviderFactory : IDataConnectionFactory
	{
		public IDataConnection NewDataConnection(WolfConnectionOptions options)
		{
			var cs = options.GetValue(WolfOptionsSqlServer.Key_ConnectionString);
			if (string.IsNullOrWhiteSpace(cs))
				throw new ArgumentException("A connection string must be specified to open a data connection to Microsoft SQL Server");
			return SqlServer.SqlServerProvider.NewSqlServerConnection(cs);
		}
	}

	public sealed class WolfOptionsSqlServer
	{
#if NETSTANDARD
        private IConfiguration _configuration;
#endif
        private WolfConnectionOptions _options = new WolfConnectionOptions();

		internal const string Key_ConnectionString = "ConnectionString";
		internal const string Key_ConnectionStringName = "ConnectionStringName";

		public WolfConnectionOptions Options { get { return this._options; } }

		/// <summary>
		/// Gets or sets the SQL Server connection string to use for the connection to the database.
		/// </summary>
		public string ConnectionString
		{
			get { return this._options.GetValue(Key_ConnectionString); }
			set { this._options[Key_ConnectionString] = value; }
		}

		/// <summary>
		/// Gets or sets the case-sensitive name of a configuration connection string found in the "ConnectionStrings" section to use for the connection to the database. Only used if "ConnectionString" is not set.
		/// </summary>
		public string ConnectionStringName
		{
			get { return this._options.GetValue(Key_ConnectionStringName); }
			set { this._options[Key_ConnectionStringName] = value; }
		}

		public WolfOptionsSqlServer()
		{
			this._options.Provider = typeof(SqlServerProviderFactory).AssemblyQualifiedName;
		}

#if NETSTANDARD
        public WolfOptionsSqlServer(IConfiguration config)
			: this()
		{
			this._configuration = config;
		}
#endif

        public void AutoConfigureFromConfiguration()
		{
            if (string.IsNullOrEmpty(this.ConnectionString))
                this.ConnectionString = this.GetConnectionStringFromCfg(this.ConnectionStringName);

			if (string.IsNullOrEmpty(this.ConnectionString))
				throw new InvalidOperationException("A connection string for the application couldn't be determined. Wolf will automatically use a connection string from your configuration providing it is the only connection string defined. Otherwise, a connection string can be defined via the options.");
		}

        internal string GetConnectionStringFromCfg(string ConnectionStringName)
        {
            string Ret = null;
#if NETSTANDARD
                if (null != this._configuration)
                {
                    // ** Automatically aquire connection string using the .NET Core style configuration source
                    var ConnectionStrings = this._configuration.GetSection("ConnectionStrings").GetChildren();
                    if (!string.IsNullOrEmpty(ConnectionStringName))
                        Ret = ConnectionStrings.FirstOrDefault(cs => cs.Key.Equals(ConnectionStringName))?.Value;
                    else if (1 == ConnectionStrings.Count())
                        Ret = ConnectionStrings.FirstOrDefault()?.Value;
                }
#endif
#if NETFRAMEWORK
            if (string.IsNullOrEmpty(Ret))
            {
                // ** Automatically aquire connection string using the .NET Framework style configuration source
                if (!string.IsNullOrEmpty(ConnectionStringName) && null != System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionStringName])
                    Ret = System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
                else if (System.Configuration.ConfigurationManager.ConnectionStrings.Count > 0)
                    Ret = System.Configuration.ConfigurationManager.ConnectionStrings[0].ConnectionString;
            }
#endif
            return Ret;
        }
    }
}