using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blacksmiths.Utils.Wolf.SqlServer.Data
{
    /// <summary>
	/// Specifies that the type is a SQL Server UDT table type (TVP)
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class UdtTableAttribute : Attribute
    {
        /// <summary>
        /// The name of the SQL Server UDT represented by the type
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    /// Specifies the column is a SQL Server UDT column
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class UdtColumnAttribute : Attribute
    {
        public int Ordinal { get; set; }
        public string Name { get; set; }           // Overrides column name
        //public SqlDbType SqlDbType { get; set; }   // Overrides inferred SQL type - commented because IL doesn't support nullable and don't need this right now
        public int Length { get; set; }             // For (N)VarChar/VarBinary; -1 means MAX or N/A since nullable can't be written down to IL
        //public byte? Precision { get; set; }        // For Decimal
        //public byte? Scale { get; set; }            // For Decimal

        public UdtColumnAttribute(int ordinal)
        {
            this.Ordinal = ordinal;
        }
    }
}
