﻿/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Generation.CSharp
{
	public class CSharpGenerator : ICodeGenerator
	{
		private Dictionary<string, string> CSharpTypes = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase)
		{
			{ "System.Boolean", "bool?" },
			{ "System.Byte", "byte?" },
			{ "System.SByte", "sbyte" },
			{ "System.Char", "char?" },
			{ "System.Decimal", "decimal?" },
			{ "System.Double", "double?" },
			{ "System.Single", "float?" },
			{ "System.Int32", "int?" },
			{ "System.UInt32", "uint?" },
			{ "System.Int64", "long?" },
			{ "System.UInt64", "ulong?" },
			{ "System.Object", "object" },
			{ "System.Int16", "short?" },
			{ "System.UInt16", "ushort?" },
			{ "System.String", "string" },
			{ "System.DateTime", "DateTime?" },
			{ "System.Guid", "Guid?" },
			{ "System.DateTimeOffset", "DateTimeOffset?" }
		};

		public Action<string> Log { get; set; }

		public EntityCollection[] GenerateCode(GenerationOptions options)
		{
			if (null == options)
				throw new ArgumentNullException(nameof(options));
			if (null == options.ConnectionOptions)
				throw new ArgumentException("Connection options must be specified");

			var Ret = new List<EntityCollection>();

			this.Sprocs = new EntityCollection()
			{
				Path = "StoredProcedures.cs",
				Generate = () => this.GenerateCode(options, options.StoredProcedures.Namespace, this.Sprocs)
			};

			this.Models = new EntityCollection()
			{
				Path = "Models.cs",
				Generate = () => this.GenerateCode(options, options.Models.Namespace, this.Models)
			};

			// ** Aquire entities
			const string ADO_SCHEMA = "SPECIFIC_SCHEMA";
			const string ADO_TYPE = "ROUTINE_TYPE";
			const string ADO_NAME = "SPECIFIC_NAME";

			var dc = (DataConnection)options.ConnectionOptions.NewDataConnection();

			using (var connection = dc.Provider.GetConnectionProvider().ToDbConnection())
			{
				connection.Open();
                WriteLog($"Querying schema for '{dc.Provider}'...");
				var procedures = connection.GetSchema("Procedures");
				var types = connection.GetSchema("DataTypes");

				var filteredProcedures = procedures.Rows.Cast<DataRow>()
						.Where(r => r[ADO_TYPE].Equals("PROCEDURE"));

				foreach (var spRow in filteredProcedures)
				{
					try
					{
						var sp = this.CreateSp(connection, types, (string)spRow[ADO_SCHEMA], (string)spRow[ADO_NAME], !options.DefaultSchema.Equals((string)spRow[ADO_SCHEMA]));

						if (options.StoredProcedures.Enabled)
						{
							this.AddEntity(Ret, new Entity()
							{
								Name = (string)spRow[ADO_NAME],
								Schema = (string)spRow[ADO_SCHEMA],
								Type = EntityType.StoredProcedure,
								Generate = () => this.GenerateCode(sp)
							});
						}

						if (options.Models.Enabled)
						{
							var ModelDef = this.CreateModelDef(dc.Provider, connection, sp);
							if (null != ModelDef)
								this.AddEntity(Ret, new Entity()
								{
									Name = (string)spRow[ADO_NAME],
									Schema = (string)spRow[ADO_SCHEMA],
									Type = EntityType.Model,
									Generate = () => this.GenerateCode(ModelDef, options)
								});
						}
					}
					catch(DbException dbex)
					{
						this.WriteLog($"ERROR: Whilst generating code for '{spRow[ADO_SCHEMA]}.{spRow[ADO_NAME]}' an error was raised from the database: {dbex.Message}");
					}
				}
			}

			return Ret.ToArray();
		}

		private EntityCollection Sprocs;
		private EntityCollection Models;

		private void AddEntity(List<EntityCollection> collections, Entity entity)
		{
			// ** single file for models and sprocs
			if (entity.Type == EntityType.StoredProcedure && null != this.Sprocs)
			{
				this.Sprocs.Add(entity);
				if (!collections.Contains(this.Sprocs))
					collections.Add(this.Sprocs);
			}
			else if(entity.Type == EntityType.Model && null != this.Models)
			{
				this.Models.Add(entity);
				if (!collections.Contains(this.Models))
					collections.Add(this.Models);
			}
		}

		private string GenerateCode(GenerationOptions options, string Namespace, EntityCollection entities)
		{
			bool Break = false;
			var generatorType = this.GetType();
			var sb = new IndentableStringBuilder();

			sb.AppendLine("//----------------------");
			sb.AppendLine("// <auto-generated>");
			sb.AppendLine($"//	This code was generated by Wolf ({generatorType.Name} {generatorType.Assembly.GetName().Version})");
			sb.AppendLine("//	https://github.com/BlacksmithsTech/Wolf");
			sb.AppendLine("// </auto-generated>");
			sb.AppendLine("//----------------------");
			sb.AppendLine("using System;");
			sb.AppendLine("using Blacksmiths.Utils.Wolf;");
			sb.AppendLine("using Blacksmiths.Utils.Wolf.Attribution;");
			sb.AppendLine();
			sb.AppendLine($"namespace {EncodeNamespace(Namespace)}");
			sb.AppendLine("{");
			sb.Indent();

			foreach (var schema in entities
				.Select(r => r.Schema)
				.Distinct()
				.OrderBy(schema => schema.Equals(options.DefaultSchema))
				.ThenBy(schema => schema))
			{
				var filteredEntities = entities
					.Where(r => r.Schema.Equals(schema))
					.OrderBy(r => r.Name);

				bool RequiresNs = !schema.Equals(options.DefaultSchema);
				if (RequiresNs)
				{
					if (Break)
					{
						sb.AppendLine();
						Break = false;
					}

					sb.AppendLine($"namespace {EncodeNamespace(schema)}");
					sb.AppendLine("{");
					sb.Indent();
				}

				foreach (var entity in filteredEntities)
				{
					if (Break)
						sb.AppendLine();

					sb.Append(entity.Generate());
					Break = true;
				}

				if (RequiresNs)
				{
					sb.Outdent();
					sb.AppendLine("}");
				}
			}

			sb.Outdent();
			sb.AppendLine("}");

			return sb.ToString();
		}

		public ModelDef CreateModelDef(IProvider provider, DbConnection connection, StoredProcedure sp)
		{
			ModelDef Ret = null;
			var Cmd = sp.GetDbCommand(provider, connection, null);
			using (var Reader = Cmd.DbCommand.ExecuteReader(CommandBehavior.SchemaOnly))
			{
				var Schema = Reader.GetSchemaTable();
				if(null != Schema)
				{
					Ret = new ModelDef();
					Ret.Name = sp.ProcedureName;

					const string ADO_COL_NAME = "ColumnName";
					const string ADO_COL_ORDINAL = "ColumnOrdinal";
					const string ADO_COL_TYPE = "DataType";
					const string ADO_COL_SIZE = "ColumnSize";
					const string ADO_ALLOW_NULL = "AllowDBNull";
                    const string ADO_DATA_TYPE_NAME = "DataTypeName";

                    foreach (var FieldRow in Schema.Rows.Cast<DataRow>().Where(r => !string.IsNullOrEmpty((string)r[ADO_COL_NAME])).OrderBy(r => (int)r[ADO_COL_ORDINAL]))
                        Ret.Add(new ModelField()
                        {
                            Name = (string)FieldRow[ADO_COL_NAME],
                            TypeName = !FieldRow.IsNull(ADO_COL_TYPE) ? ((Type)FieldRow[ADO_COL_TYPE]).FullName : (string)FieldRow[ADO_DATA_TYPE_NAME],
                            AllowNulls = (bool)FieldRow[ADO_ALLOW_NULL],
                            Length = (int)FieldRow[ADO_COL_SIZE],
                            Commented = FieldRow.IsNull(ADO_COL_TYPE)
                        });
				}
			}
			return Ret;
		}

		public StoredProcedure CreateSp(DbConnection connection, DataTable types, string SchemaName, string ProcName, bool RequiresNs)
		{
			const string ADO_LENGTH = "CHARACTER_MAXIMUM_LENGTH";

			var Ret = new StoredProcedure(RequiresNs ? $"[{SchemaName}].[{ProcName}]" : $"[{ProcName}]");
			var Schema = connection.GetSchema("ProcedureParameters", new string[] { null, SchemaName, ProcName });
			foreach (var Row in Schema.Rows.Cast<DataRow>().OrderBy(r => r["ORDINAL_POSITION"]))
			{
				var ParamName = (string)Row["PARAMETER_NAME"];
				var Param = new StoredProcedure.CodeGenSpParameter(ParamName.Trim('@'));

				var TypeRow = types.Rows.Cast<DataRow>().FirstOrDefault(r => r["TypeName"].Equals(Row["DATA_TYPE"]));

				if (null != TypeRow)
				{
					Param.ValueTypeName = (string)TypeRow["DataType"];
				}
				else
				{
					this.WriteLog($"WARNING: Data type for {Ret.ProcedureName}{ParamName} could not be determined. Will assume string during generation");
					Param.ValueTypeName = "System.String";
				}

				switch ((string)Row["PARAMETER_MODE"])
				{
					case "IN":
						Param.Direction = ParameterDirection.Input;
						break;

					case "OUT":
						Param.Direction = ParameterDirection.Output;
						break;

					case "INOUT":
						Param.Direction = ParameterDirection.InputOutput;
						break;
				}

				if (!Row.IsNull(ADO_LENGTH))
					Param.Length = (int)Row[ADO_LENGTH];

				Ret.AddParameter(Param);
			}
			return Ret;
		}

		private string GenerateCode(StoredProcedure sp)
		{
			var sb = new IndentableStringBuilder();

			var SpName = Utility.QualifiedSqlName.Parse(sp.ProcedureName);
			var ClassName = EncodeSymbol(SpName.Name);
			if (!SpName.Name.Equals(ClassName) || !string.IsNullOrEmpty(SpName.Schema))
				sb.AppendLine($@"[Procedure(Name = ""{sp.ProcedureName}"")]");
			sb.AppendLine($"public class {ClassName} : StoredProcedure");
			sb.AppendLine("{");
			sb.Indent();
			foreach (var Param in sp.OfType<StoredProcedure.CodeGenSpParameter>())
			{
				var ParamName = EncodeSymbol(Param.Name);

				var ValueTypeName = Param.ValueTypeName;
				var ParamType = this.CSharpTypes.ContainsKey(ValueTypeName) ? this.CSharpTypes[ValueTypeName] : ValueTypeName;

				string AttrName = null;
				string AttrLength = null;
				string AttrDirection = null;

				if (!Param.Name.Equals(ParamName))
					AttrName = $@"Name = ""{Param.Name}""";
				if (Param.Length > -1)
					AttrLength = $@"Length = {Param.Length}";
				if (Param.Direction != ParameterDirection.Input)
					AttrDirection = $@"Direction = System.Data.ParameterDirection.{Param.Direction}";

				var AttrParams = String.Join(", ", new[] { AttrName, AttrLength, AttrDirection }.Where(a => null != a));
				if (!string.IsNullOrEmpty(AttrParams))
					sb.AppendLine($@"[Parameter({AttrParams})]");

				sb.AppendLine($"public {ParamType} {ParamName} {{ get; set; }}");
			}
			sb.Outdent();
			sb.AppendLine("}");

			return sb.ToString();
		}

		private string GenerateCode(ModelDef md, GenerationOptions options)
		{
			var sb = new IndentableStringBuilder();
			var MdlName = Utility.QualifiedSqlName.Parse(md.Name);

			var ClassName = EncodeSymbol(MdlName.Name);
			if (!MdlName.Name.Equals(ClassName) || !string.IsNullOrEmpty(MdlName.Schema))
				sb.AppendLine($@"[Source(From = ""{md.Name}"")]");
			sb.AppendLine($"public class {ClassName}");
			sb.AppendLine("{");
			sb.Indent();
			foreach (var Field in md)
			{
				var FieldName = EncodeSymbol(Field.Name);

				var ValueTypeName = Field.TypeName;
				var ParamType = this.CSharpTypes.ContainsKey(ValueTypeName) ? this.CSharpTypes[ValueTypeName] : ValueTypeName;
                var Comment = Field.Commented ? "//" : string.Empty;
				var TypeDef = Type.GetType(ValueTypeName);

				string AttrName = null;
				string AttrLength = null;
				string AttrNullable = null;

				if (!Field.Name.Equals(FieldName))
					AttrName = $@"From = ""{Field.Name}""";
				if (Field.Length > -1 && ValueTypeName.Equals("System.String", StringComparison.CurrentCultureIgnoreCase))
					AttrLength = $@"Length = {Field.Length}";
                if (!Field.AllowNulls)
                {
					if (!TypeDef.IsValueType)
						AttrNullable = "Nullable = false";
                    ParamType = ParamType.Trim('?');
                }

				var AttrParamsSource = String.Join(", ", new[] { AttrName }.Where(a => null != a));
				if (!string.IsNullOrEmpty(AttrParamsSource))
					sb.AppendLine($@"{Comment}[Source({AttrParamsSource})]");
				var AttrParamsConstraints = String.Join(", ", new[] { AttrLength,AttrNullable }.Where(a => null != a));
				if (!string.IsNullOrEmpty(AttrParamsConstraints))
					sb.AppendLine($@"{Comment}[Constraint({AttrParamsConstraints})]");
				var Virtual = options.Models.VirtualMembers ? "virtual " : string.Empty;
				sb.AppendLine($"{Comment}public {Virtual}{ParamType} {FieldName} {{ get; set; }}");
			}
			sb.Outdent();
			sb.AppendLine("}");

			return sb.ToString();
		}

		private string EncodeNamespace(string value)
		{
			return Regex.Replace(value.Trim(), @"[ ]", "_", RegexOptions.IgnoreCase);
		}

		private string EncodeSymbol(string value)
		{
			return Regex.Replace(value.Trim(), @"[ .-]", "_", RegexOptions.IgnoreCase);
		}

		private void WriteLog(string log)
		{
			this.Log?.Invoke(log);
		}
	}
}
