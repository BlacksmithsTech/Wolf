using System;
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
				return DataConnection.FromOptions(sqlOptions);
			});

			return services;
		}
	}
}

namespace Blacksmiths.Utils.Wolf.Utility
{
	public sealed class WolfOptionsSqlServer : WolfOptions
	{
		private IConfiguration _configuration;

		/// <summary>
		/// Gets or sets the SQL Server connection string to use for the connection to the database.
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		/// Gets or sets the case-sensitive name of a configuration connection string found in the "ConnectionStrings" section to use for the connection to the database. Only used if "ConnectionString" is not set.
		/// </summary>
		public string ConnectionStringName { get; set; }

		public WolfOptionsSqlServer(IConfiguration config)
		{
			this._configuration = config;
		}

		public override IDataConnection NewDataConnection()
		{
			if(string.IsNullOrEmpty(this.ConnectionString))
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

			return SqlServer.SqlServerProvider.NewSqlServerConnection(this.ConnectionString);
		}
	}
}