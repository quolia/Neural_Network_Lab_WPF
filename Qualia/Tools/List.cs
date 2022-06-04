using System.Collections.Generic;

namespace Tools
{
    public class ListX<T> : List<T> where T : class
    {
        public ListX(int capacity)
            : base(capacity)
        {
            //
        }

        public ListX(IEnumerable<T> collection)
            : base(collection)
        {
            //
        }

        public new void Add(T obj)
        {
            if (Count == 0)
            {
                (this as List<T>).Add(obj);
            }
            else if (obj is ListNode<T>)
            {
                var last = Last() as ListNode<T>;
                last.Next = obj;
                (obj as ListNode<T>).Previous = Last();
                (this as List<T>).Add(obj);
            }
            else
            {
                (this as List<T>).Add(obj);
            }
        }

        public T Last() => this[Count - 1];

        public bool Any() => Count > 0;

        // Fisher-Yates shuffle.
        public void Shuffle()
        {
            int n = Count;
            while (n > 1)
            {
                n--;
                int k = Rand.Flat.Next(n + 1);
                (this[k], this[n]) = (this[n], this[k]);
            }
        }
    }

    unsafe public class ListNode<T>
    {
        public T Next;
        public T Previous;
    }
}
