﻿using System;
using Blacksmiths.Utils.Wolf;
using Blacksmiths.Utils.Wolf.Attribution;

namespace Blacksmiths.Tests.Wolf.Sprocs
{
	namespace HumanResources
	{
		[Procedure(Name = "[HumanResources].[uspSubtract]")]
		public class uspSubtract : StoredProcedure
		{
			public int? Value1 { get; set; }
			public int? Value2 { get; set; }
			[Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
			public int? Calculation { get; set; }
		}

		[Procedure(Name = "[HumanResources].[uspUpdateEmployeeHireInfo]")]
		public class uspUpdateEmployeeHireInfo : StoredProcedure
		{
			public int? BusinessEntityID { get; set; }
			[Parameter(Length = 50)]
			public string JobTitle { get; set; }
			public DateTime? HireDate { get; set; }
			public DateTime? RateChangeDate { get; set; }
			public decimal? Rate { get; set; }
			public sbyte PayFrequency { get; set; }
			public bool? CurrentFlag { get; set; }
		}

		[Procedure(Name = "[HumanResources].[uspUpdateEmployeeLogin]")]
		public class uspUpdateEmployeeLogin : StoredProcedure
		{
			public int? BusinessEntityID { get; set; }
			[Parameter(Length = 892)]
			public string OrganizationNode { get; set; }
			[Parameter(Length = 256)]
			public string LoginID { get; set; }
			[Parameter(Length = 50)]
			public string JobTitle { get; set; }
			public DateTime? HireDate { get; set; }
			public bool? CurrentFlag { get; set; }
		}

		[Procedure(Name = "[HumanResources].[uspUpdateEmployeePersonalInfo]")]
		public class uspUpdateEmployeePersonalInfo : StoredProcedure
		{
			public int? BusinessEntityID { get; set; }
			[Parameter(Length = 15)]
			public string NationalIDNumber { get; set; }
			public DateTime? BirthDate { get; set; }
			[Parameter(Length = 1)]
			public string MaritalStatus { get; set; }
			[Parameter(Length = 1)]
			public string Gender { get; set; }
		}
	}

	public class uspAdd : StoredProcedure
	{
		public int? Value1 { get; set; }
		public int? Value2 { get; set; }
		[Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
		public int? Calculation { get; set; }
	}

	public class uspConcatenate : StoredProcedure
	{
		[Parameter(Length = 50)]
		public string Value1 { get; set; }
		[Parameter(Length = 50)]
		public string Value2 { get; set; }
		[Parameter(Length = 100, Direction = System.Data.ParameterDirection.InputOutput)]
		public string Concatenated { get; set; }
	}

	public class uspGetBillOfMaterials : StoredProcedure
	{
		public int? StartProductID { get; set; }
		public DateTime? CheckDate { get; set; }
	}

	public class uspGetDepartments : StoredProcedure
	{
	}

	public class uspGetEmployeeManagers : StoredProcedure
	{
		public int? BusinessEntityID { get; set; }
	}

	public class uspGetManagerEmployees : StoredProcedure
	{
		public int? BusinessEntityID { get; set; }
	}

	public class uspGetPeople : StoredProcedure
	{
	}

	public class uspGetWhereUsedProductID : StoredProcedure
	{
		public int? StartProductID { get; set; }
		public DateTime? CheckDate { get; set; }
	}

	public class uspLogError : StoredProcedure
	{
		[Parameter(Direction = System.Data.ParameterDirection.InputOutput)]
		public int? ErrorLogID { get; set; }
	}

	public class uspPrintError : StoredProcedure
	{
	}

	public class uspSearchCandidateResumes : StoredProcedure
	{
		[Parameter(Length = 1000)]
		public string searchString { get; set; }
		public bool? useInflectional { get; set; }
		public bool? useThesaurus { get; set; }
		public int? language { get; set; }
	}

	[Procedure(Name = "[uspValidate Password]")]
	public class uspValidate_Password : StoredProcedure
	{
		[Parameter(Length = 20)]
		public string Password { get; set; }
	}
}