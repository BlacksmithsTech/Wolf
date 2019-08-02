using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Generation
{
	public class GenerationOptions
	{
		/// <summary>
		/// The database connection options used to collect schema
		/// </summary>
		public Utility.WolfOptions ConnectionOptions { get; set; }

		/// <summary>
		/// The base namespace to place generated code into
		/// </summary>
		public string Namespace { get; set; } = "Blacksmiths.Utils.Wolf.StoredProcedures";

		/// <summary>
		/// Specifies the default schema used when connecting to a database. Used to simplify the resulting namespace of classes used.
		/// </summary>
		public string DefaultSchema { get; set; } = "dbo";
	}
}
