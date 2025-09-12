/*
 * Wolf
 * A .NET Standard data access layer component
 *
 * (C) 2019 Blacksmiths Technology Ltd
 */
using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;

namespace Blacksmiths.Utils.Wolf.Utility
{
	/// <summary>
	/// Binds an ADO.NET data command to it's associated wolf request object, it's parameters, and results
	/// </summary>
	public sealed class WolfCommandBinding : IDisposable
	{
		private bool bound;
		private bool? _isConsumed;
		private CommandDefinition? _commandDefinition;
		//public CommandDefinition CommandDefinition { get; set; }
		public DataRequestItem RequestItem { get; set; }
		internal bool isConsumed
		{
			get => this._isConsumed ?? (this.Reader?.IsConsumed).GetValueOrDefault();
			set => this._isConsumed = value;
		}

		internal SqlMapper.GridReader Reader { get; set; }
		internal Exception Exception { get; set; }
		internal CommandDefinition SetCommandDefinition(IProvider provider, DbConnection connection, DbTransaction transaction, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
		{
			if (this._commandDefinition.HasValue)
				throw new InvalidOperationException("The command definition is already set");
			this._commandDefinition = this.RequestItem.Command.ToCommandDefinition(provider, connection, transaction, commandTimeoutSeconds, cancellationToken);
			return this._commandDefinition.Value;
        }

		internal void Bind()
		{
			if (!bound														// Not yet bound
				&& this.RequestItem.Command is IStoredProcedure sp			// Command was a stored procedure
				&& this._commandDefinition.HasValue							// Command was executed
				&& null == this.Exception)									// Command didn't throw
			{
				sp.SetDynamicParameters((DynamicParameters)this._commandDefinition.Value.Parameters);
				bound = true;
			}
		}

        //internal CommandDefinition ToCommandDefinition(IProvider provider, DbConnection connection, DbTransaction transaction, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
        //	=> this.RequestItem.Command.ToCommandDefinition(provider, connection, transaction, commandTimeoutSeconds, cancellationToken);

        public void Dispose()
        {
			this.Reader?.Dispose();
            this.Bind();
        }
    }
}
