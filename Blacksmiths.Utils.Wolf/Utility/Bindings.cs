/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Utility
{
	/// <summary>
	/// Binds an ADO.NET data command to it's associated wolf request object, it's parameters, and results
	/// </summary>
	public sealed class WolfCommandBinding
	{
        internal const string C_EXTENDED_WOLF_COMMAND = "_WolfCommand";

		public DbCommand DbCommand { get; set; }
		public IDataRequestItem WolfRequestItem { get; set; }
		public WolfParameterDbBinding[] Parameters { get; set; }
		internal DataSet ResultData { get; private set; }

        public WolfCommandBinding()
        {
            this.ResultData = new DataSet();
        }

		/// <summary>
		/// Updates Wolf request values with ADO.NET Values
		/// </summary>
		public void Bind()
		{
			if (null != this.ResultData)
				for (int i = 0; i < this.ResultData.Tables.Count; i++)
				{
					//var QualifiedName = Utility.StringHelpers.GetQualifiedSpName(this.WolfRequestItem.TableName);
					//if (!string.IsNullOrEmpty(QualifiedName.Schema))
					//	this.ResultData.Tables[i].Namespace = QualifiedName.Schema;
					//if (!string.IsNullOrEmpty(QualifiedName.Name))
					//	this.ResultData.Tables[i].TableName = 0 == i ? QualifiedName.Name : $"{QualifiedName.Name}{i}";

					this.ResultData.Tables[i].Namespace = this.WolfRequestItem.TableName.Schema;
                    this.ResultData.Tables[i].TableName = 0 == i ? this.WolfRequestItem.TableName.Name : $"{this.WolfRequestItem.TableName.Name}{i}";
                    this.ResultData.Tables[i].ExtendedProperties[C_EXTENDED_WOLF_COMMAND] = this;
                }

			if (null != this.Parameters)
				foreach (var p in this.Parameters)
					if (null != p)
						p.Bind();
		}
	}

	/// <summary>
	/// Binds an ADO.NET data parameter to its associated wolf parameter
	/// </summary>
	public sealed class WolfParameterDbBinding
	{
		public DbParameter DbParameter { get; set; }
		public StoredProcedure.SpParameter WolfParameter { get; set; }

		/// <summary>
		/// Updates the Wolf parameter value with the ADO.NET value
		/// </summary>
		public void Bind()
		{
			WolfParameter.Value = !DBNull.Value.Equals(DbParameter.Value) ? DbParameter.Value : null;
		}
	}
}
