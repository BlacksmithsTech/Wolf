﻿using System;
using System.Collections.Generic;
using System.Text;
using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.Utility;
using Microsoft.Extensions.Configuration;
using System.Linq;

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
		private IConfiguration _configuration;
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

		public WolfOptionsSqlServer(IConfiguration config)
			: this()
		{
			this._configuration = config;
		}

		public void AutoConfigureFromConfiguration()
		{
			if(string.IsNullOrEmpty(this.ConnectionString) && null != this._configuration)
			{
				// ** Automatically aquire connection string
				var ConnectionStrings = this._configuration.GetSection("ConnectionStrings").GetChildren();
				if (!string.IsNullOrEmpty(this.ConnectionStringName))
					this.ConnectionString = ConnectionStrings.FirstOrDefault(cs => cs.Key.Equals(this.ConnectionStringName))?.Value;
				else if (1 == ConnectionStrings.Count())
					this.ConnectionString = ConnectionStrings.FirstOrDefault()?.Value;
			}

			if (string.IsNullOrEmpty(this.ConnectionString))
				throw new InvalidOperationException("A connection string for the application couldn't be determined. Wolf will automatically use a connection string from your configuration providing it is the only connection string defined. Otherwise, a connection string can be defined via the options.");
		}
	}
}