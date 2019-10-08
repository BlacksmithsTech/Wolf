# Wolf
Wolf is a database-first lightweight data access layer which prioritises ease of use and productivity in the middle tier. It provides basic object-relational mapping features and a fluent interface.

Features:
* Support for Microsoft SQL Server 2005-2019
* Relational data mapping from stored procedure data and parameters
* Transactional relational change submission
* Portable class library
    * .NET Standard 2.0 (for usage with .NET/ASP.NET Core)
    * .NET Framework 4.0 (for usage with .NET Framework/ASP.NET Webforms and MVC)
* Support for ADO.NET DataSet and DataTable
* Code generation from provided Windows/Mac/Linux CLI
    * Strongly typed stored procedures
    * Data models using stored procedures

Use Wolf when:
* You develop database-first
* Your application uses stored procedures. Wolf, by-design, does not allow ad-hoc queries to be written on your application/middle tiers
* You'd like to your data models to be submitted to the database within a transaction automatically
* You'd like an API which is more explicit about when the database is being utilised
* You're looking for the benefits of newer ORM functionality whilst still supporting ADO.NET DataSet and DataTable

Don't use Wolf right now if:
* Your application needs to connect to multiple databases
* Your database isn't Microsoft SQL Server
* You need out-of-the-box logging/performance metrics
* You need built-in caching

## Quick Start

*NuGet package manager console*
```powershell
Install-Package Blacksmiths.Utils.Wolf.SqlServer
```
### ASP.NET Core

*Startup.cs*
```csharp
public void ConfigureServices(IServiceCollection services)
{
    ...
    services.AddWolfSqlServer();
}
```
*Usage.cs*
```csharp
using System;
...
using Blacksmiths.Utils.Wolf;

namespace MyApp.Pages
{
    public class IndexModel : PageModel
    {
        public IndexModel(IDataConnection connection)
        {
            // Example usage from strongly typed stored procedure generated using Wolf CLI
            var Model = connection.NewRequest()
                .Add(new StoredProcedures.uspGetManagerEmployees() { BusinessEntityID = 2 })
                .Execute()
                .ToSimpleModel<Models.uspGetManagerEmployees>();
        }
    }
}
```

### ASP.NET Webforms / MVC

*Global.asax.cs*
```csharp
using System;
...
using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.SqlServer;

namespace MyApp
{
    public class Global : HttpApplication
    {
        public static IDataConnection Connection { get; private set; }

        void Application_Start(object sender, EventArgs e)
        {
            Connection = SqlServerProvider.NewSqlServerConnectionFromCfg();
        }
    }
}
```
### Download the CLI tools
A command line tool is available to generate C# code for your stored procedures, both in terms of their parameters and result sets.

[View releases](https://github.com/BlacksmithsTech/Wolf/releases)

*Command line*
```
wolf -cs="Server=(localdb)\mssqllocaldb;Database=AdventureWorks2016;Integrated Security=true"
```