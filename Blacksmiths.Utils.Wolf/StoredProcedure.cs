/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using Blacksmiths.Utils.Wolf.Utility;

namespace Blacksmiths.Utils.Wolf
{
	public class StoredProcedure : IDataRequestItem, IEnumerable<StoredProcedure.SpParameter>
	{
		// *************************************************
		// Inner objects
		// *************************************************

		public class SpParameter
		{
			private object _value;

			/// <summary>
			/// The name of the parameter, as declared on the database
			/// </summary>
			public string Name { get; set; }

			/// <summary>
			/// Gets or sets the value of the parameter
			/// </summary>
			public virtual object Value
			{
				get { return this._value; }
				set { this._value = value; }
			}

			public System.Data.ParameterDirection Direction { get; set; }

			public int Length { get; set; } = -1;

			/// <summary>
			/// Optional. Gets or sets the data type of the parameter. If one is not supplied, a type is inferred from the value where one has been provided. If you expect to handle nulls, this property should be set.
			/// </summary>
			public DbType? ValueType { get; set; }

			public SpParameter(string name, ParameterDirection direction = ParameterDirection.Input, object value = null)
			{
				this.Name = name;
				this._value = value;
				this.Direction = direction;
			}

			protected void InferValueType(Type t)
			{
				var typeMap = new Dictionary<Type, DbType>();
				typeMap[typeof(byte)] = DbType.Byte;
				typeMap[typeof(sbyte)] = DbType.Int16;//SByte won't re-map
				typeMap[typeof(short)] = DbType.Int16;
				typeMap[typeof(ushort)] = DbType.UInt16;
				typeMap[typeof(int)] = DbType.Int32;
				typeMap[typeof(uint)] = DbType.UInt32;
				typeMap[typeof(long)] = DbType.Int64;
				typeMap[typeof(ulong)] = DbType.UInt64;
				typeMap[typeof(float)] = DbType.Single;
				typeMap[typeof(double)] = DbType.Double;
				typeMap[typeof(decimal)] = DbType.Decimal;
				typeMap[typeof(bool)] = DbType.Boolean;
				typeMap[typeof(string)] = DbType.String;
				typeMap[typeof(char)] = DbType.StringFixedLength;
				typeMap[typeof(Guid)] = DbType.Guid;
				typeMap[typeof(DateTime)] = DbType.DateTime;
				typeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
				typeMap[typeof(byte[])] = DbType.Binary;
				typeMap[typeof(byte?)] = DbType.Byte;
				typeMap[typeof(sbyte?)] = DbType.SByte;
				typeMap[typeof(short?)] = DbType.Int16;
				typeMap[typeof(ushort?)] = DbType.UInt16;
				typeMap[typeof(int?)] = DbType.Int32;
				typeMap[typeof(uint?)] = DbType.UInt32;
				typeMap[typeof(long?)] = DbType.Int64;
				typeMap[typeof(ulong?)] = DbType.UInt64;
				typeMap[typeof(float?)] = DbType.Single;
				typeMap[typeof(double?)] = DbType.Double;
				typeMap[typeof(decimal?)] = DbType.Decimal;
				typeMap[typeof(bool?)] = DbType.Boolean;
				typeMap[typeof(char?)] = DbType.StringFixedLength;
				typeMap[typeof(Guid?)] = DbType.Guid;
				typeMap[typeof(DateTime?)] = DbType.DateTime;
				typeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;

				if (typeMap.ContainsKey(t))
					this.ValueType = typeMap[t];
			}
		}

		public class SpParameter<T> : SpParameter
		{
			public new T Value
			{
				get { return (T)base.Value; }
				set { base.Value = value; }
			}

			public SpParameter(string name, System.Data.ParameterDirection direction = System.Data.ParameterDirection.Input, T value = default)
				: base(name, direction, value)
			{
				this.InferValueType(typeof(T));
			}
		}

		public class BoundSpParameter : SpParameter
		{
			private object _instance;
			private System.Reflection.PropertyInfo _property;

			public override object Value
			{
				get { return this._property.GetValue(this._instance, null); }
				set { this._property.SetValue(this._instance, value, null); }
			}

			public BoundSpParameter(string name, object instance, System.Reflection.PropertyInfo property)
				: base(name)
			{
				this._instance = instance;
				this._property = property;
				this.InferValueType(this._property.PropertyType);
			}
		}

		internal class CodeGenSpParameter : SpParameter
		{
			private string _ValueTypeName;

			internal string ValueTypeName
			{
				get { return this._ValueTypeName; }
				set
				{
					this._ValueTypeName = value;
					this.InferValueType(Type.GetType(this._ValueTypeName));
				}
			}

			internal CodeGenSpParameter(string name)
				: base(name) { }
		}

		// *************************************************
		// Fields
		// *************************************************

		private List<SpParameter> _dbParameters;
		private string _procedureName;
		private Utility.QualifiedSqlName _targetTableName;

		// *************************************************
		// Properties
		// *************************************************

		/// <summary>
		/// Gets or sets the stored procedure name
		/// </summary>
		[Attribution.Ignore]
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
		internal List<SpParameter> DbParameters
		{
			get
			{
				if(null == this._dbParameters)
				{
					this._dbParameters = new List<SpParameter>();
					this.Reflect();
				}
				return this._dbParameters;
			}
		}

		/// <summary>
		/// Gets or sets the current return value of the stored procedure
		/// </summary>
		[Attribution.Parameter(Direction = System.Data.ParameterDirection.ReturnValue)]
		public int? ReturnValue { get; set; }

		QualifiedSqlName IDataRequestItem.TableName
		{
			get { return this._targetTableName ?? Utility.QualifiedSqlName.Parse(this.ProcedureName); }
			set { this._targetTableName = value; }
		}

		// *************************************************
		// Indexer
		// *************************************************

		/// <summary>
		/// Gets the parameter with the given name
		/// </summary>
		/// <param name="parameterName">Name of the parameter</param>
		/// <returns>The database parameter</returns>
		[Attribution.Ignore]
		public SpParameter this[string parameterName]
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
			return this.AddParameter(new SpParameter<T>(name, direction, value));
		}

		/// <summary>
		/// Adds a parameter to the stored procedure
		/// </summary>
		/// <param name="p">Parameter to add</param>
		public StoredProcedure AddParameter(SpParameter p)
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

		public IEnumerator<SpParameter> GetEnumerator()
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

			var ProcAttrib = thisType.GetCustomAttributes(typeof(Attribution.Procedure), true).Cast<Attribution.Procedure>().FirstOrDefault();
			if(null != ProcAttrib)
			{
				this._procedureName = ProcAttrib.Name;
			}

			if (null == this._procedureName)
				this._procedureName = $"[{thisType.Name}]";

			if (null == this._dbParameters)
				foreach (var member in thisType.GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
				{
					if (member.GetCustomAttributes(typeof(Attribution.Ignore), true).Length > 0)
						continue;

					var ParamName = member.Name;
					var ParamType = member.PropertyType;
					var ParamDirection = System.Data.ParameterDirection.Input;
					int? ParamLength = null;

					var ParamAttrib = member.GetCustomAttributes(typeof(Attribution.Parameter), true).Cast<Attribution.Parameter>().FirstOrDefault();
					if (null != ParamAttrib)
					{
						ParamName = ParamAttrib.Name ?? ParamName;
						ParamLength = ParamAttrib.Length;
						ParamDirection = ParamAttrib.Direction;
					}

					var p = new BoundSpParameter(ParamName, this, member);
					p.Direction = ParamDirection;
					p.Length = ParamLength.GetValueOrDefault(-1);
					this.AddParameter(p);
				}
		}
	}

	public static class StoredProcedureExtentions
	{
		public static IFluentResultSp<T> Execute<T>(this T sp, IDataConnection connection) where T : StoredProcedure
		{
			return (IFluentResultSp<T>)connection.NewRequest().Add(sp).Execute(new DataResultSp<T>());
		}
	}
}
