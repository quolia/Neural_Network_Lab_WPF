using Qualia;
using System;
using System.Collections.Generic;

namespace Tools
{
    public class ListX<T> : List<T> where T : ListXNode<T>
    {
        //public T First { get; private set; }
        public T Last { get; private set; }

        internal T FirstOrDefault()
        {
            if (Last == null)
            {
                return null;
            }

            return this[0];
        }

        //private T[] _array = Array.Empty<T>();

        public ListX(int capacity)
            : base(capacity)
        {
            //
        }

        public ListX(IEnumerable<T> collection)
            : base(collection)
        {
            if (Count > 0)
            {
                Last = this[Count - 1];
            }
        }

        /*
        public new T this[int index]
        {
            get
            {
                return _array[index];
            }

            set
            {
                _array[index] = value;
            }
        }
        */
        

        public new void Add(T node)
        {
            base.Add(node);

            if (Count > 1)
            {
                Last.Next = node;
                node.Previous = Last;
            }

            Last = node;
        }

        //public T Last() => this[Count - 1];

        //public bool Any() => Count > 0;

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

        internal T FirstOrDefault(Predicate<T> value)
        {
            return Find(value);
        }

        internal bool Any()
        {
            return Last != null;
        }

        internal bool Any(Predicate<T> value)
        {
            return Find(value) != null;
        }

        internal int CountIf(Func<T, bool> value)
        {
            int count = 0;

            var node = this[0];

            while (node != null)
            {
                if (value(node))
                {
                    ++count;
                }

                node = node.Next;
            }

            return count;
        }

        internal float Max(Func<T, float> value)
        {
            float max = 0;

            var node = this[0];
            while (node != null)
            {
                var v = value(node);
                if (v > max)
                {
                    max = v;
                }

                node = node.Next;
            }

            return max;
        }
    }

    unsafe public class ListXNode<T>
    {
        public T Next;
        public T Previous;
    }
}
