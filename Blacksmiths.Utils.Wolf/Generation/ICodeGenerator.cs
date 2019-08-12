using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Generation
{
	public interface ICodeGenerator
	{
		Action<string> Log { get; set; }
		EntityCollection[] GenerateCode(GenerationOptions options);
	}
}
