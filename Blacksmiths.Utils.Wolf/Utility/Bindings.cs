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
		private bool? _isConsumed;

		//public CommandDefinition CommandDefinition { get; set; }
		public DataRequestItem RequestItem { get; set; }
		internal bool isConsumed
		{
			get => this._isConsumed ?? (this.Reader?.IsConsumed).GetValueOrDefault();
			set => this._isConsumed = value;
		}

		internal SqlMapper.GridReader Reader { get; set; }

		internal CommandDefinition ToCommandDefinition(IProvider provider, DbConnection connection, DbTransaction transaction, int? commandTimeoutSeconds = null, CancellationToken cancellationToken = default)
			=> this.RequestItem.Command.ToCommandDefinition(provider, connection, transaction, commandTimeoutSeconds, cancellationToken);

        public void Dispose()
        {
			this.Reader?.Dispose();
        }
    }
}
