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

    public class GroupOfTests
    {
        public int ID { get; set; }

        [Relation(nameof(ID))]
        public GroupExtras Extras { get; set; }
    }

    public class GroupExtras
    {
        public int ID { get; set; }
        public string Name { get; set; }
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

        [Relation(nameof(AddressID))]
        public PersonAddress Address { get; set; }
	}

    [Source(From = "uspGetPersonAddress")]
    public class PersonAddress
    {
        [Constraint(Nullable = false)]
        public int AddressID { get; set; }
        [Constraint(Length = 60, Nullable = false)]
        public string AddressLine1 { get; set; }
        [Constraint(Length = 60)]
        public string AddressLine2 { get; set; }
        [Constraint(Length = 30, Nullable = false)]
        public string City { get; set; }
        [Constraint(Nullable = false)]
        public int StateProvinceID { get; set; }
        [Constraint(Length = 15, Nullable = false)]
        public string PostalCode { get; set; }
        [Constraint(Nullable = false)]
        public Guid rowguid { get; set; }
        [Constraint(Nullable = false)]
        public DateTime ModifiedDate { get; set; }
    }
}
