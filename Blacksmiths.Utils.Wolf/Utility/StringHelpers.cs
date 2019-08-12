using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Blacksmiths.Utils.Wolf.Utility
{
	public static class StringHelpers
	{
		public static (string Schema, string Name) GetQualifiedSpName(string Name)
		{
			var m = Regex.Match(Name, @"^(?:\[(?<schema>[^\n\r\[\]]+)]\.)*\[(?<name>[^\n\r\[\]]+)]$");
			if (m.Success)
				return (m.Groups["schema"].Value, m.Groups["name"].Value ?? Name);
			else
				return (null, Name);
		}
	}
}
