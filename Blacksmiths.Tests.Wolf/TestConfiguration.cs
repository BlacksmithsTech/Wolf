using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmiths.Tests.Wolf
{
    internal static class TestConfiguration
    {
        private static readonly Lazy<IConfigurationRoot> _cfg = new(() =>
            new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                //.AddUserSecrets(typeof(TestConfiguration).Assembly, optional: true)
                .AddEnvironmentVariables()
                .Build()
        );

        internal static IConfiguration Configuration => _cfg.Value;
    }
}
