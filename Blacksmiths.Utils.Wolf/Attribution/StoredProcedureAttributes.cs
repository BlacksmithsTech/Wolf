﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Attribution
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public sealed class Parameter : Attribute
	{
		/// <summary>
		/// Gets or sets the name of the parameter exactly as it is declared on the database
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets the length, if applicable, of the parameter
		/// </summary>
		public int Length { get; set; } = -1; //nullable can't be written down to IL

		/// <summary>
		/// Gets or sets the parameter data direction
		/// </summary>
		public System.Data.ParameterDirection Direction { get; set; }
	}

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public sealed class Ignore : Attribute
	{
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class Procedure : Attribute
	{
		/// <summary>
		/// Gets or sets the name of the stored procedure exactly as it is declared on the database
		/// </summary>
		public string Name { get; set; }
	}
}
