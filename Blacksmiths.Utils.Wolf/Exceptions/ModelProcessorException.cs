using System;
using System.Collections.Generic;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Exceptions
{
	public sealed class ModelProcessorException : WolfException
	{
		public Model.ResultModel Model { get; private set; }
		public System.Data.DataRow AffectedRow { get; set; }
		public ModelProcessorException(Model.ResultModel model, string message, Exception innerException = null)
			: base(message, innerException)
        {
			this.Model = model;
        }
	}
}
