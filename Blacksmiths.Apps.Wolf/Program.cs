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
using System.Collections.Generic;

namespace Blacksmiths.Apps.Wolf
{
	class Program
	{
        const string CfgPathKeyName = "ConfigurationProfilePath";
        static GenerationOptions Options;

		static int Main(string[] args)
		{
			try
			{
                Console.WriteLine("Wolf Command Line Interface");
                Console.WriteLine("(C) Blacksmiths Technology 2019");
                Console.WriteLine();

				Initialise(args);

                if (ValidateConfiguration(args))
                {
                    var Depot = new GenerationDepot();
                    Depot.Log = (msg) =>
                    {
                        Console.WriteLine(msg);
                    };
                    Depot.Generate(Options);
                }

				return 0;
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex);
				return 1;
			}
		}

        static bool ValidateConfiguration(string[] args)
        {
            if(args.Length > 0 && new[] { "-?", "/?"}.Contains(args[0]))
            {
                DisplayUsage();
                return false;
            }

            const string NoConnection = "No connection string was specified. A connection string can be specified on the command line using /cs= or specified in your configuration file";

            if (null == Options.ConnectionOptions || Options.ConnectionOptions.ConfigurationIsEmpty())
            {
                if (0 == args.Length)
                {
                    DisplayUsage();
                    return false;
                }
                else
                {
                    Console.WriteLine(NoConnection);
                    return false;
                }
            }

            return true;
        }

        static void DisplayUsage()
        {
            Console.WriteLine("Wolf CLI provides code-generation for database entities such as stored procedures and data models.");
            Console.WriteLine();
            Console.WriteLine("WOLF [-cfg=config.json] [-cs=\"connection string\"] [-out=\"path\"]");
            Console.WriteLine("Usage:");
            Console.WriteLine("\t-cfg=\tSpecifies the path to a JSON configuration file");
            Console.WriteLine("\t-cs=\tSpecifies a database connection string (defaults to using the Microsoft SQL Server provider)");
            Console.WriteLine("\t-out=\tSpecifies a path (directory) where the generated code will be written");
        }

        static void Initialise(string[] args)
		{
			AddConfiguration(args);
		}


        static void AddConfiguration(string[] args)
		{
            var stage1configBuilder = new ConfigurationBuilder();
            AddCommandLineSwitches(args, stage1configBuilder);
            var stage1config = stage1configBuilder.Build();

            var userConfigurationPath = stage1config[CfgPathKeyName];

			var stage2configBuilder = new ConfigurationBuilder()
				.SetBasePath(System.IO.Directory.GetCurrentDirectory())
				.AddEnvironmentVariables()
				.AddJsonFile("appsettings.json", true);

			if (!string.IsNullOrEmpty(userConfigurationPath))
				stage2configBuilder.AddJsonFile(userConfigurationPath, false);

            AddCommandLineSwitches(args, stage2configBuilder);

			Options = new GenerationOptions();
			stage2configBuilder.Build().Bind(Options);
		}

        static void AddCommandLineSwitches(string[] args, IConfigurationBuilder b)
        {
            b.AddCommandLine(args, new Dictionary<string, string>()
            {
                { "-cfg",  CfgPathKeyName},
                { "-cs", "ConnectionOptions:ConnectionString" },
                { "-out", "Output:Path" }
            });
        }
	}
}
