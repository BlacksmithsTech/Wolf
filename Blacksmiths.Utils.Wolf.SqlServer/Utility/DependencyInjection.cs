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
        /// <summary>
        /// Adds Wolf (using SQL Server) to your application IServiceCollection as a scoped service. Your connection is then accessible using DI from Blacksmiths.Utils.Wolf.IDataConnection
        /// </summary>
        /// <param name="options">Specifies Wolf options, such as your connection string</param>
        /// <returns></returns>
        public static IServiceCollection AddWolfSqlServer(this IServiceCollection services, Action<WolfOptionsSqlServer> options = null)
        {
            services.AddScoped<IDataConnection>((provider) =>
            {
                var sqlOptions = new WolfOptionsSqlServer(provider.GetRequiredService<IConfiguration>());
                if (null != options)
                    options(sqlOptions);

                sqlOptions.AutoConfigureFromConfiguration();

                var connection = DataConnection.FromOptions(sqlOptions.Options);
                if (connection is IServiceLocator serviceLocatorConnection)
                    serviceLocatorConnection.ServiceProvider = provider;

                return connection;
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
        public bool ConfigurationIsEmpty(WolfConnectionOptions options)
        {
            return string.IsNullOrEmpty(options.GetValue(WolfOptionsSqlServer.Key_ConnectionString));
        }

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
                {
                    Ret = ConnectionStrings.FirstOrDefault(cs => cs.Key.Equals(ConnectionStringName))?.Value;
                    if (null == Ret)
                        throw new ArgumentException($"The connection string '{ConnectionStringName}' was not present in your configuration (using .NET Core style [JSON] configuration source)");
                }
                else
                {
                    if (1 == ConnectionStrings.Count())
                        Ret = ConnectionStrings.FirstOrDefault()?.Value;
                }
            }
#endif
#if NETFRAMEWORK
            if (string.IsNullOrEmpty(Ret))
            {
                const string BuiltInKeyName = "LocalSqlServer";

                // ** Automatically aquire connection string using the .NET Framework style configuration source
                if (!string.IsNullOrEmpty(ConnectionStringName))
                {
                    if (null != System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionStringName])
                        Ret = System.Configuration.ConfigurationManager.ConnectionStrings[ConnectionStringName].ConnectionString;
                    else
                        throw new ArgumentException($"The connection string '{ConnectionStringName}' was not present in your configuration (using .NET Framework style [XML] configuration source)");
                }
                else
                {
                    if (1 == System.Configuration.ConfigurationManager.ConnectionStrings.Count)
                        Ret = System.Configuration.ConfigurationManager.ConnectionStrings[0].ConnectionString;
                    else if (2 == System.Configuration.ConfigurationManager.ConnectionStrings.Count && null != System.Configuration.ConfigurationManager.ConnectionStrings[BuiltInKeyName])
                        Ret = System.Configuration.ConfigurationManager.ConnectionStrings[1].ConnectionString;//.NET Framework provides "LocalSqlServer" out-of-the-box and this needs ignoring if another string is present in the collection
                }
                
            }
#endif
            return Ret;
        }
    }
}