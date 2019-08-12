using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Attribution
{
	/// <summary>
	/// Configures the source of data population for the given class, property or field. This attribute can be used multiple times if there are multiple possible alternative sources.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = true)]
	public class Source : Attribute
	{
		/// <summary>
		/// For classes specify the exact name of a request item used to fetch the data. 
		/// For non-collection properties and fields, specify the exact name of a column to populate the member.
		/// Does not apply to collections, where you should use the "Relation" attribute instead
		/// </summary>
		public string From { get; set; }
	}

	/// <summary>
	/// Defines database constraints for the given property or field
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
	public class Constraint : Attribute
	{
		public bool Nullable { get; set; } = true;
		public int Length { get; set; } = -1;
	}
}
