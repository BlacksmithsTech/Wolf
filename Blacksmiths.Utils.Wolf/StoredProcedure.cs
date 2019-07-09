/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace Blacksmiths.Utils.Wolf
{
	public class StoredProcedure : IDataRequestItem, IEnumerable<StoredProcedure.Parameter>
	{
		// *************************************************
		// Inner objects
		// *************************************************

		public class Parameter
		{
			public string Name { get; private set; }

			public object Value { get; set; }

			public System.Data.ParameterDirection Direction { get; private set; }

			public Parameter(string name, System.Data.ParameterDirection direction = System.Data.ParameterDirection.Input, object value = null)
			{
				this.Name = name;
				this.Value = value;
				this.Direction = direction;
			}
		}

		public class Parameter<T> : Parameter
		{
			public new T Value
			{
				get { return (T)base.Value; }
				set { base.Value = value; }
			}

			public Parameter(string name, System.Data.ParameterDirection direction = System.Data.ParameterDirection.Input, T value = default)
				: base(name, direction, value) { }
		}

		// *************************************************
		// Fields
		// *************************************************

		private List<Parameter> _dbParameters;
		private string _procedureName;

		// *************************************************
		// Properties
		// *************************************************

		/// <summary>
		/// Gets or sets the stored procedure name
		/// </summary>
		public string ProcedureName
		{
			get
			{
				if(null == this._procedureName)
				{
					this.Reflect();
				}

				return this._procedureName;
			}
			set
			{
				if (null == value)
					throw new ArgumentNullException("Procedure name may not be null");
				this._procedureName = value;
			}
		}

		/// <summary>
		/// Gets the collection of parameters to be sent to the database
		/// </summary>
		internal List<Parameter> DbParameters
		{
			get
			{
				if(null == this._dbParameters)
				{
					this._dbParameters = new List<Parameter>();
					this.Reflect();
				}
				return this._dbParameters;
			}
		}

		// *************************************************
		// Indexer
		// *************************************************

		/// <summary>
		/// Gets the parameter with the given name
		/// </summary>
		/// <param name="parameterName">Name of the parameter</param>
		/// <returns>The database parameter</returns>
		public Parameter this[string parameterName]
		{
			get
			{
				return this.DbParameters.FirstOrDefault(p => p.Name.Equals(parameterName));
			}
		}

		// *************************************************
		// Constructor
		// *************************************************

		/// <summary>
		/// Creates new stored procedure
		/// </summary>
		/// <param name="name">Name of the stored procedure</param>
		public StoredProcedure(string name = null)
		{
			if (null != name)
				this.ProcedureName = name;
		}

		// *************************************************
		// Methods
		// *************************************************

		/// <summary>
		/// Adds an output parameter to the stored procedure by specifying its name
		/// </summary>
		/// <typeparam name="T">Type of parameter to add</typeparam>
		/// <param name="name">Name of parameter</param>
		public StoredProcedure AddOutputParameter<T>(string name)
		{
			return this.AddParameter<T>(name, direction: System.Data.ParameterDirection.Output);
		}

		/// <summary>
		/// Adds a parameter to the stored procedure by specifying its name and initial value
		/// </summary>
		/// <param name="p">Name of parameter</param>
		public StoredProcedure AddParameter<T>(string name, T value = default, System.Data.ParameterDirection direction = System.Data.ParameterDirection.Input)
		{
			return this.AddParameter(new Parameter<T>(name, direction, value));
		}

		/// <summary>
		/// Adds a parameter to the stored procedure
		/// </summary>
		/// <param name="p">Parameter to add</param>
		public StoredProcedure AddParameter(Parameter p)
		{
			if (null == p)
				throw new ArgumentNullException("DbParameter may not be null");
			if (null != this[p.Name])
				throw new ArgumentException($"A parameter with the name '{p.Name}' already exists in this stored procedure");
			this.DbParameters.Add(p);
			return this;
		}

		/// <summary>
		/// Gets the value of a parameter of the specified type
		/// </summary>
		/// <typeparam name="T">Type of the value expected</typeparam>
		/// <param name="parameterName">Name of the parameter to fetch</param>
		/// <returns>The value of the parameter</returns>
		public T GetParameterValue<T>(string parameterName)
		{
			var p = this[parameterName];
			if (null == p)
				throw new ArgumentException($"No such parameter '{parameterName}'");
			return (T)p.Value;
		}

		/// <summary>
		/// Removes a parameter from the stored procedure
		/// </summary>
		/// <param name="name">Name of the parameter to remove</param>
		public void RemoveParameter(string name)
		{
			var p = this[name];
			if (null == p)
				throw new ArgumentException($"A parameter with the name '{name}' was not found to remove");
			this.DbParameters.Remove(p);
		}

		/// <summary>
		/// Convienience method to execute this stored procedure
		/// </summary>
		/// <param name="connection"></param>
		/// <returns></returns>
		public IFluentResultSp Execute(DataConnection connection)
		{
			return new DataResultSp((DataResult)connection.NewRequest().Add(this).Execute());
		}

		// *************************************************
		// Contract (IDataRequestItem)
		// *************************************************

		public Utility.WolfCommandBinding GetDbCommand(IProvider provider, DbConnection connection)
		{
			return provider.GetStoredProcedureProvider().ToDbCommand(this, connection);
		}

		// *************************************************
		// Contract (IEnumerable)
		// *************************************************

		public IEnumerator<Parameter> GetEnumerator()
		{
			return this.DbParameters.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// *************************************************
		// Utility
		// *************************************************

		/// <summary>
		/// Reflect on this instance and discover parameters
		/// </summary>
		private void Reflect()
		{
			var thisType = this.GetType();

			if (null == this._procedureName)
				this._procedureName = thisType.Name;
		}
	}
}
