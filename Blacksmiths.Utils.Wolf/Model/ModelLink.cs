using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Model
{
	internal sealed class ModelLink
	{

		private Dictionary<string, MemberLink> _members;
		private List<MemberLink> _keyColumns;
		private Dictionary<DataRow, object> _addedRows;

		internal MemberLink this[string Name]
		{
			get
			{
				return this._members[Name];
			}
		}

		internal IEnumerable<MemberLink> Members { get { return this._members.Values; } }
		internal DataTable Data { get; private set; }
		internal ModelDefinition ModelDefinition { get; private set; }

		/// <summary>
		/// Gets the PK columns if they are known, or otherwise returns all members as the key
		/// </summary>
		internal IEnumerable<MemberLink> KeyColumns { get { return this._keyColumns.Count > 0 ? this._keyColumns : this.Members; } }
		internal ModelLink(ModelDefinition md, DataTable dt)
		{
			this.ModelDefinition = md;
			this.Data = dt;
			ModelLinkCollection.AttachModelLink(this, this.Data);
			this._members = this.ModelDefinition.TypeDefinition.GetLinkedMembersFor(this.Data);
			this.IdentifyKeyColumns(null);
		}

		private void IdentifyKeyColumns(DataConnection connection)
		{
			// ** Identify the key columns
			try
			{
				this._keyColumns = new List<MemberLink>(this._members.Values.Where(m => this.Data.PrimaryKey.Any(pkc => pkc == m.Column)));//Attribution based PKs?
				if (0 == this._keyColumns.Count && null != connection)
				{
					// ** Attribution based PK (reflection is expensive, so moved this into the null!=connection so this only happens during updates and not reads)
					this._keyColumns = new List<MemberLink>(this._members.Values.Where(m => m.IsKey(Attribution.Key.KeyType.PrimaryKey)));

					if (0 == this._keyColumns.Count)
					{
						connection.FetchSchema(this.Data); // If a connection has been provided, go to the database to identify the key columns
						this._keyColumns = new List<MemberLink>(this._members.Values.Where(m => this.Data.PrimaryKey.Any(pkc => pkc == m.Column))); //use the new schema data to populate
					}
					else if (null == this.Data.PrimaryKey || 0 == this.Data.PrimaryKey.Length)
					{
						this.Data.PrimaryKey = this._keyColumns.Select(kc => kc.Column).ToArray();
						//this.Data.Constraints.Add(new UniqueConstraint(this.Data.PrimaryKey));
					}
				}
			}
			catch(Exception ex)
			{
				throw new Exceptions.ModelException($"An exception was thrown whilst identifying key columns for {Utility.DataTableHelpers.GetNormalisedName(this.Data)}", ex);
			}
		}

		internal void ThrowIfCantUpdate(DataConnection connection)
		{
			if (0 == this._keyColumns.Count) //Re-identify. Key information is loaded late for perf reasons (may not need PK info if the user is only doing a read)
				this.IdentifyKeyColumns(connection);

			if (this.Data.Rows.Count > 0)
			{
				if (0 == this._keyColumns.Count) //This will prevent commits to keyless tables for now, but will see if anyone needs this before allowing users to accidentally express their UPDATEs as DELETE+INSERT.
					throw new InvalidOperationException("Your model contains existing data which potentially to UPDATE, but there is no primary key defined.");
			}
		}

		//internal DataView GetDataView(ModelLink parentModelLink, object parent)
		//{
		//    Utility.PerfDebuggers.BeginTrace("Obtaining DataView");

		//    var Ret = new DataView(this.Data);

		//    if (null != this.ModelDefinition.ParentModel)
		//    {
		//        // ** Apply relationship
		//        var Relationship = this.FindFirstValidRelationshipWithParent(parentModelLink);

		//        var sb = new StringBuilder();
		//        foreach (var ChildKeyName in Relationship.ChildFieldNames)
		//        {
		//            if (sb.Length > 0)
		//                sb.Append(" AND ");
		//            sb.Append($"{ChildKeyName} = '{parentModelLink[ChildKeyName].GetValue(parent)}'");//TODO: Encode
		//        }
		//        Ret.RowFilter = sb.ToString();
		//    }

		//    Utility.PerfDebuggers.EndTrace("Obtaining DataView");

		//    return Ret;
		//}

		internal bool ContainsMember(string Name)
		{
			return this._members.ContainsKey(Name);
		}

		internal IComparable GetKey(object source, string[] KeyNames)
		{
			switch (KeyNames.Length)
			{
				case 1:
					return new Tuple<object>(this[KeyNames[0]].GetValue(source));
				case 2:
					return new Tuple<object, object>(this[KeyNames[0]].GetValue(source), this[KeyNames[1]].GetValue(source));
				case 3:
					return new Tuple<object, object, object>(this[KeyNames[0]].GetValue(source), this[KeyNames[1]].GetValue(source), this[KeyNames[2]].GetValue(source));
				default:
					throw new InvalidOperationException("Unsupported key length");
			}
		}

		internal object[] GetKeyValues(object source, string[] keyNames = null)
		{
			if (null == keyNames)
				keyNames = this._keyColumns.Select(kc => kc.Member.Name).ToArray();
			return keyNames.Select(kn => this[kn].GetValue(source)).ToArray();
		}

		internal Attribution.Relation FindFirstValidRelationshipWithParent(ModelLink parentLink)
		{
			foreach (var Relation in this.ModelDefinition.RelationshipAttributes.Value)
			{
				if (Relation.IsSane()
					&& Relation.ParentFieldNames.All(fn => parentLink.ContainsMember(fn))
					&& Relation.ChildFieldNames.All(fn => this.ContainsMember(fn)))
				{
					return Relation;
				}
			}
			return null;
		}

		internal IEnumerable<MemberLink> GetAllMembers(string[] memberNames)
		{
			if (memberNames.All(mn => this.ContainsMember(mn)))
				return this._members.Where(mkvp => memberNames.Contains(mkvp.Key)).Select(mkvp => mkvp.Value);
			else
				return null;
		}

		internal IEnumerable<Attribution.ForeignKey> GetForeignKeys()
		{
			return this._members.Values.SelectMany(m => m.GetForeignKeys());
		}

		internal void RememberAddedRow(DataRow r, object o)
		{
			if (null == this._addedRows)
				this._addedRows = new Dictionary<DataRow, object>();
			this._addedRows.Add(r, o);
		}

		//internal Dictionary<DataRow, object> FlushAddedRows()
		//{
		//    var ret = this._addedRows ?? new Dictionary<DataRow, object>();
		//    this._addedRows = null;
		//    return ret;
		//}

		internal void ApplyIdentityValue(DataRow row)
		{
			/*
             * During the commit, ADO.NET retrieves updated field values, such as the identity, from INSERT commands.
             * This re-applies those changes back to the source object model.
             */
			if (null != this._addedRows && this._addedRows.ContainsKey(row))
				ResultModel.BoxObject(this._addedRows[row], this, row);
		}
	}
}