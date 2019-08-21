using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    public class ListX<T> : List<T> where T : class
    {
        public ListX(int capacity)
            : base(capacity)
        {
            //
        }

        T[] Array;

        public new T this[int index]
        {
            get => Array[index];
        }

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

            RefreshArray();
        }

        private void RefreshArray()
        {
            Array = new T[Count];
            for (int i = 0; i < Count; ++i)
            {
                Array[i] = base[i];
            }
        }
    }

    public class ListNode<T>
    {
        public T Next;
        public T Previous;
    }
}
