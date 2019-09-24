using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Linq;

namespace Blacksmiths.Utils.Wolf.Model
{
    //internal sealed class TypeRelationship
    //{
    //    internal TypeDefinition ParentTypeLink;
    //    internal Utility.MemberAccessor MemberAccessor;
    //    internal MemberInfo Member;
    //    internal Type MemberType;
    //    internal Type CollectionType;

    //    internal TypeRelationship(TypeDefinition parentTl, MemberInfo m)
    //    {
    //        this.ParentTypeLink = parentTl;
    //        this.Member = m;
    //        this.MemberAccessor = Utility.MemberAccessor.Create(this.Member);
    //        this.MemberType = Utility.ReflectionHelper.GetMemberType(this.Member);
    //        this.CollectionType = Utility.ReflectionHelper.GetCollectionType(this.Member) ?? this.MemberType;
    //    }

    //    internal void SetValue(object source, IEnumerable<object> collection)
    //    {
    //        if (this.MemberType.IsArray)
    //            this.MemberAccessor.SetValue(source, Utility.ReflectionHelper.ArrayFromList(this.CollectionType, collection.ToArray()));
    //        else if (typeof(System.Collections.IList).IsAssignableFrom(this.MemberType))
    //            this.MemberAccessor.SetValue(source, collection);
    //        else
    //            this.MemberAccessor.SetValue(source, collection.FirstOrDefault());
    //    }

    //    internal void SetValue(object source, System.Collections.IList list)
    //    {
    //        this.MemberAccessor.SetValue(source, list);
    //    }

        
    //}
}
