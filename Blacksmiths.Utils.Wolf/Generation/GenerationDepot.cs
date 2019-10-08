/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Blacksmiths.Utils.Wolf.Generation
{
	public class GenerationDepot
	{
		public Action<string> Log { get; set; }

		public void Generate(GenerationOptions options)
		{
			options.Normalise();

			//TODO: Allow extensible generators via configuration
			ICodeGenerator Generator = new CSharp.CSharpGenerator();
			Generator.Log = this.Log;
			var EntityCollections = Generator.GenerateCode(options);

            //TODO: Configurable output behaviours
            foreach (var collection in EntityCollections)
            {
                var p = Path.Combine(options.Output.Path, collection.Path);
                File.WriteAllText(p, collection.Generate());
                this.Log?.Invoke($"Wrote {collection.Count:N0} entities to '{p}'");
            }
		}
	}
}
