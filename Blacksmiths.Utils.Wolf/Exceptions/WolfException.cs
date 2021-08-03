using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Exceptions
{
	public abstract class WolfException : Exception
	{
		public WolfException(string message)
			: base(message) { }

		public WolfException(string message, Exception innerException)
			: base(message, innerException) { }
	}
}
