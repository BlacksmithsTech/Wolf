using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Generation
{
	public enum EntityType
	{
		StoredProcedure,
		Model,
	}

	public sealed class Entity
	{
		public string Schema { get; set; }
		public string Name { get; set; }
		public EntityType Type { get; set; }
		public Func<string> Generate { get; set; }
	}

	public sealed class EntityCollection : List<Entity>
	{
		public string Path { get; set; }
		public Func<string> Generate { get; set; }
	}
}
