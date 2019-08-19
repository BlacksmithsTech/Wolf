/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Generation
{
	public class GenerationOptions
	{
		public Wolf.Utility.WolfConnectionOptions ConnectionOptions { get; set; }

		/// <summary>
		/// Specifies the default schema used when connecting to a database. Used to simplify the resulting namespace of classes used.
		/// </summary>
		public string DefaultSchema { get; set; } = "dbo";

		public StoredProcedureOptions StoredProcedures { get; set; } = new StoredProcedureOptions();

		public ModelOptions Models { get; set; } = new ModelOptions();

		public OutputOptions Output { get; set; } = new OutputOptions();

		public void Normalise()
		{
			this.Output.Normalise();
		}
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

	public class OutputOptions
	{
		/// <summary>
		/// Output path
		/// </summary>
		public string Path { get; set; } = string.Empty;

		public void Normalise()
		{
			this.Path = this.Path.Replace('/', System.IO.Path.DirectorySeparatorChar);
			if (!string.IsNullOrEmpty(this.Path) && !this.Path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
				this.Path += System.IO.Path.DirectorySeparatorChar;
		}
	}
}
