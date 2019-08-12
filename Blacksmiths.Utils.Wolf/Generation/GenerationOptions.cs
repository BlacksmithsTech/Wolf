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
		/// Specifies the default schema used when connecting to a database. Used to simplify the resulting namespace of classes used.
		/// </summary>
		public string DefaultSchema { get; set; } = "dbo";

		public StoredProcedureOptions StoredProcedures { get; set; } = new StoredProcedureOptions();

		public ModelOptions Models { get; set; } = new ModelOptions();
	}

	public class StoredProcedureOptions
	{
		/// <summary>
		/// Gets or sets if the generation is enabled
		/// </summary>
		public bool Enabled { get; set; } = true;

		/// <summary>
		/// The base namespace to place generated code into
		/// </summary>
		public string Namespace { get; set; } = "Blacksmiths.Utils.Wolf.StoredProcedures";
	}

	public class ModelOptions
	{
		/// <summary>
		/// Gets or sets if the generation is enabled
		/// </summary>
		public bool Enabled { get; set; } = true;


		/// <summary>
		/// The base namespace to place generated code into
		/// </summary>
		public string Namespace { get; set; } = "Blacksmiths.Utils.Wolf.Models";
	}
}
