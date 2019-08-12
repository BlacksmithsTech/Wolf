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

	class uspGetManagerEmployees
	{
		public int RecursionLevel;
		public string OrganizationNode;
		public string ManagerFirstName;
		public string ManagerLastName;
		public int BusinessEntityId;
		public string FirstName;
		public string LastName;
	}
}
