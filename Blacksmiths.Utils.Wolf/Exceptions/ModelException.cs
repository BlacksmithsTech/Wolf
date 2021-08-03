using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Exceptions
{
	public sealed class ModelException : WolfException
	{
		public ModelException(string message, Exception innerException)
			: base(message, innerException) { }
	}
}
