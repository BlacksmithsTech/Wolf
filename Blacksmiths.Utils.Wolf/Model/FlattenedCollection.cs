using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Data;
using System.Collections;

namespace Blacksmiths.Utils.Wolf.Model
{
    internal class FlattenedCollection : IEnumerable<LinkedRange>
    {
        private DataSet _ds;
        private List<LinkedRange> _ranges;
        private LinkedRange _lastRange;

        internal System.Collections.IList Collection { get; private set; }

        //internal ModelLink this[int i]
        //{
        //    get
        //    {
        //        return this._ranges.FirstOrDefault(r => r.StartIndex <= i && r.Length >= i)?.ModelLink;
        //    }
        //}

        internal FlattenedCollection(DataSet ds, ModelDefinition md, object source, System.Collections.IList collection)
        {
            this._ranges = new List<LinkedRange>();
            this._ds = ds;
            this.AddCollection(md, source, collection);
        }

        internal IEnumerable<object> GetCollectionRange(LinkedRange r)
        {
            return this.Collection.Cast<object>().Skip(r.StartIndex).Take(r.Length);
        }

        //internal object FindObject(DataRow row)
        //{
        //    object result = null;
        //    foreach (var range in this)
        //    {
        //        result = range.ModelLink.ModelDefinition.TypeDefinition.FindObject(row, this.GetCollectionRange(range), range.ModelLink.KeyColumns);
        //        if (null != result)
        //            break;
        //    }
        //    return result;
        //}

        internal void AddCollection(ModelDefinition md, object source, System.Collections.IList collection)
        {
            if (null == collection)
                return;

            int startIndex;
            int length;
            if(null == this.Collection)
            {
                this.Collection = collection;
                startIndex = 0;
                length = this.Collection.Count;
            }
            else
            {
                var thisCollection = collection;
                var a = this.Collection as Array ?? this.Collection.Cast<object>().ToArray();
                var b = thisCollection as Array ?? thisCollection.Cast<object>().ToArray();
                startIndex = a.Length;
                length = b.Length;

                if (b.Length > 0)
                {
                    var t = new object[a.Length + b.Length];
                    Array.Copy(a, 0, t, 0, a.Length);
                    Array.Copy(b, 0, t, a.Length, b.Length);
                    this.Collection = t;
                }
            }

            this.AddRange(startIndex, length, md.GetModelTarget(this._ds));
        }

        private void AddRange(int index, int length, ModelLink ml)
        {
            if (ml == this._lastRange?.ModelLink)
            {
                this._lastRange.Length += length;
            }
            else
            {
                this._lastRange = new LinkedRange(index, length, ml);
                this._ranges.Add(this._lastRange);
            }
        }

        public IEnumerator<LinkedRange> GetEnumerator()
        {
            return this._ranges.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this._ranges.GetEnumerator();
        }
    }

    internal class LinkedRange
    {
        internal int StartIndex { get; set; }
        internal int Length { get; set; }
        internal ModelLink ModelLink { get; set; }

        internal LinkedRange(int index, int length, ModelLink ml)
        {
            this.StartIndex = index;
            this.Length = length;
            this.ModelLink = ml;
        }
    }
}
