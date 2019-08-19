/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Microsoft.Extensions.Configuration;
using System;
using Blacksmiths.Utils.Wolf.Generation;
using System.Linq;

namespace Blacksmiths.Apps.Wolf
{
	class Program
	{
		static GenerationOptions Options;

		static int Main(string[] args)
		{
			try
			{
				Initialise(args);

				var Depot = new GenerationDepot();
				Depot.Log = (msg) =>
				{
					Console.WriteLine(msg);
				};
				Depot.Generate(Options);

				return 0;
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
				return 1;
			}
		}

		static void Initialise(string[] args)
		{
			AddConfiguration(args);
		}

		static void AddConfiguration(string[] args)
		{
			var stage1configBuilder = new ConfigurationBuilder()
				.AddCommandLine(args);
			var stage1config = stage1configBuilder.Build();
			var userConfigurationPath = stage1config["file"];

			var stage2configBuilder = new ConfigurationBuilder()
				.SetBasePath(System.IO.Directory.GetCurrentDirectory())
				.AddEnvironmentVariables()
				.AddJsonFile("appsettings.json", true);

			if (!string.IsNullOrEmpty(userConfigurationPath))
				stage2configBuilder.AddJsonFile(userConfigurationPath, true);

			stage2configBuilder.AddCommandLine(args);

			Options = new GenerationOptions();
			stage2configBuilder.Build().Bind(Options);
		}
	}
}
