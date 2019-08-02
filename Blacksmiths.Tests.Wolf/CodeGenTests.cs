using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using Blacksmiths.Tests.Wolf.Models;
using System.Diagnostics;
using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.Generation;

namespace Blacksmiths.Tests.Wolf
{
	[TestClass]
	public class CodeGenTests
	{
		[TestMethod]
		public void GenerateCSharp()
		{
			var Options = new GenerationOptions();
			var ConnectionOptions = new Blacksmiths.Utils.Wolf.Utility.WolfOptionsSqlServer(null);
			ConnectionOptions.ConnectionString = @"Server=(localdb)\mssqllocaldb;Database=AdventureWorks2016;Integrated Security=true";
			Options.ConnectionOptions = ConnectionOptions;

			var Result = new Blacksmiths.Utils.Wolf.Generation.CSharp.CSharpGenerator().GenerateCode(Options);
		}
	}
}
