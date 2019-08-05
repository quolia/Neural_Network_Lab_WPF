using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    public class ListX<T> : List<T> where T : class
    {
        public T RandomElementTrimEnd(int count = 0)
        {
            return this[Rand.Flat.Next(Count - count)];
        }

        public new void Add(T obj)
        {
            if (Count == 0)
            {
                (this as List<T>).Add(obj);
            }
            else if (obj is ListNode<T>)
            {
                var last = this.Last() as ListNode<T>;
                last.Next = obj;
                (obj as ListNode<T>).Previous = this.Last();
                (this as List<T>).Add(obj);
            }
            else
            {
                (this as List<T>).Add(obj);
            }
        }
    }

    public class ListNode<T>
    {
        public T Next;
        public T Previous;
    }
}
