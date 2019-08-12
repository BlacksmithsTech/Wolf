using Blacksmiths.Utils.Wolf.Attribution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Tests.Wolf.Models
{
	class Test
	{
		public int ID;
		public string Name;

		public Test()
		{
		}

		public Test(int id, string name)
		{
			this.ID = id;
			this.Name = name;
		}
	}

	[Source(From = "uspGetManagerEmployees")]
	class uspGetManagerEmployeesManuallyWritten
	{
		public int RecursionLevel { get; set; }
		public string OrganizationNode { get; set; }
		public string ManagerFirstName { get; set; }
		public string ManagerLastName;
		public int BusinessEntityId { get; set; }
		public string FirstName { get; set; }
		public string LastName { get; set; }
	}
}
