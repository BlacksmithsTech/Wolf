using System;
using System.Collections.Generic;

namespace Blacksmiths.Tests.Wolf.EF.Models
{
    public partial class AddressType
    {
        public AddressType()
        {
            BusinessEntityAddress = new HashSet<BusinessEntityAddress>();
        }

        public int AddressTypeId { get; set; }
        public string Name { get; set; }
        public Guid Rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }

        public virtual ICollection<BusinessEntityAddress> BusinessEntityAddress { get; set; }
    }
}
