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
			{ "System.Byte", "byte" },
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
		};

		public Action<string> Log { get; set; }

		public string GenerateCode(GenerationOptions options)
		{
			if (null == options)
				throw new ArgumentNullException(nameof(options));
			if (null == options.ConnectionOptions)
				throw new ArgumentException("Connection options must be specified");

			const string ADO_SCHEMA = "SPECIFIC_SCHEMA";
			const string ADO_TYPE = "ROUTINE_TYPE";
			const string ADO_NAME = "SPECIFIC_NAME";
			bool Break = false;

			var dc = (DataConnection)DataConnection.FromOptions(options.ConnectionOptions);
			var sb = new IndentableStringBuilder();

			sb.AppendLine("using System;");
			sb.AppendLine("using Blacksmiths.Utils.Wolf;");
			sb.AppendLine("using Blacksmiths.Utils.Wolf.Attribution;");
			sb.AppendLine();
			sb.AppendLine($"namespace {EncodeNamespace(options.Namespace)}");
			sb.AppendLine("{");
			sb.Indent();
			using (var connection = dc.Provider.GetConnectionProvider().ToDbConnection())
			{
				connection.Open();
				var procedures = connection.GetSchema("Procedures");
				var types = connection.GetSchema("DataTypes");

				foreach (var schema in procedures.Rows.Cast<DataRow>()
					.Select(r => (string)r[ADO_SCHEMA])
					.Distinct()
					.OrderBy(schema => schema.Equals(options.DefaultSchema))
					.ThenBy(schema => schema))
				{
					var schemaProcedures = procedures.Rows.Cast<DataRow>()
						.Where(r => r[ADO_SCHEMA].Equals(schema)
							&& r[ADO_TYPE].Equals("PROCEDURE"))
						.OrderBy(r => (string)r[ADO_NAME]);

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

					foreach (var spRow in schemaProcedures)
					{
						if (Break)
							sb.AppendLine();

						var sp = this.CreateSp(connection, types, (string)spRow[ADO_SCHEMA], (string)spRow[ADO_NAME], RequiresNs);
						sb.Append(this.GenerateCode(sp));
						Break = true;
					}

					if (RequiresNs)
					{
						sb.Outdent();
						sb.AppendLine("}");
					}
				}
			}
			sb.Outdent();
			sb.AppendLine("}");

			return sb.ToString();
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
					Param.ValueTypeName = (string)TypeRow["DataType"];
				else
					this.WriteLog($"WARNING: Data type for {Ret.ProcedureName}{ParamName} could not be determined. Will assume string during generation");

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

		private (string Schema, string Name) GetQualifiedSpName(string Name)
		{
			var m = Regex.Match(Name, @"^(?:\[(?<schema>[^\n\r\[\]]+)]\.)*\[(?<name>[^\n\r\[\]]+)]$");
			if (m.Success)
				return (m.Groups["schema"].Value, m.Groups["name"].Value ?? Name);
			else
				return (null, Name);
		}
		private string GenerateCode(StoredProcedure sp)
		{
			var sb = new IndentableStringBuilder();

			var SpName = this.GetQualifiedSpName(sp.ProcedureName);
			var ClassName = EncodeSymbol(SpName.Name);
			if (!SpName.Name.Equals(ClassName) || !string.IsNullOrEmpty(SpName.Schema))
				sb.AppendLine($@"[Procedure(Name = ""{sp.ProcedureName}"")]");
			sb.AppendLine($"public class {ClassName} : StoredProcedure");
			sb.AppendLine("{");
			sb.Indent();
			foreach (StoredProcedure.CodeGenSpParameter Param in sp)
			{
				var ParamName = EncodeSymbol(Param.Name);

				var ValueTypeName = Param.ValueTypeName ?? "System.String";
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

		private string EncodeNamespace(string value)
		{
			return Regex.Replace(value.Trim(), @"[ ]", "_", RegexOptions.IgnoreCase);
		}

		private string EncodeSymbol(string value)
		{
			return Regex.Replace(value.Trim(), @"[ .]", "_", RegexOptions.IgnoreCase);
		}

		private void WriteLog(string log)
		{
			this.Log?.Invoke(log);
		}
	}
}
