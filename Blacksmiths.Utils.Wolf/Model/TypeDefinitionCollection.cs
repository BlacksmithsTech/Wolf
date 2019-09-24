using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal class TypeDefinitionCollection : List<TypeDefinition>
    {
        internal TypeDefinition GetOrCreate(Type t)
        {
            var Ret = this.FirstOrDefault(tl => tl.Type.Equals(t));
            if(null == Ret)
            {
                Ret = new TypeDefinition(t, this);
                this.Add(Ret);
            }
            return Ret;
        }
    }
}
