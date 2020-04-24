using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal sealed class ModelLinkCollection
    {
        private const string C_EXTENDED_WOLF_MODELLINKS = "_WolfModelLinks";

        private List<ModelLink> _links = new List<ModelLink>();

        private ModelLinkCollection()
        {
        }

        internal static void AttachModelLink(ModelLink ml, DataTable dt)
        {
            if (!dt.ExtendedProperties.Contains(C_EXTENDED_WOLF_MODELLINKS))
                dt.ExtendedProperties.Add(C_EXTENDED_WOLF_MODELLINKS, new ModelLinkCollection());
            var mlc = FromDataTable(dt);
            mlc._links.Add(ml);
        }

        internal static ModelLinkCollection FromDataTable(DataTable dt)
        {
            return (ModelLinkCollection)dt.ExtendedProperties[C_EXTENDED_WOLF_MODELLINKS];
        }

        internal Dictionary<DataRow, object> FlushAddedRows()
        {
            if (this._links.Count == 1)
                return this._links[0].FlushAddedRows();

            var ret = new Dictionary<DataRow, object>();
            foreach (var ml in this._links)
                foreach (var flush in ml.FlushAddedRows())
                    ret.Add(flush.Key, flush.Value);
            return ret;
        }

        internal void ApplyIdentityValue(object identity, object model)
        {
            if (this._links.Count > 0)
                this._links[0].ApplyIdentityValue(identity, model);//TODO: there's a code structure problem here I think
        }
    }
}
