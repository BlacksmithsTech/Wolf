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

	[Source(From = "uspGetBusinessEntities")]
	class BusinessEntityNoRelationshipAttribution
	{
		public int BusinessEntityID { get; set; }
		public Guid rowguid { get; set; }
		public DateTime ModifiedDate { get; set; }
		public BusinessEntityAddress[] BusinessEntityAddresses { get; set; }
	}

	[Source(From = "uspGetBusinessEntities")]
	class BusinessEntity
	{
		public int BusinessEntityID { get; set; }
		public Guid rowguid { get; set; }
		public DateTime ModifiedDate { get; set; }

		[Relation(nameof(BusinessEntityID))]
		public BusinessEntityAddress[] BusinessEntityAddresses { get; set; }
	}

	[Source(From = "uspGetBusinessEntityAddresses")]
	class BusinessEntityAddress
	{
		public int BusinessEntityID { get; set; }
		public int AddressID { get; set; }
		public int AddressTypeID { get; set; }
		public Guid rowguid { get; set; }
		public DateTime ModifiedDate { get; set; }
	}
}
