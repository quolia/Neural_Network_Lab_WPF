using System;
using System.Collections.Generic;

namespace Tools
{
    public class ListX<T> : List<T> where T : ListXNode<T>
    {
        //public T First { get; private set; }
        public T Last { get; private set; }

        private T[] _array;

        public ListX(int capacity)
            : base(capacity)
        {
            //
        }

        public ListX(IEnumerable<T> collection)
            : base(collection)
        {
            FillArray();

            if (Count > 0)
            {
                //First = this[0];
                Last = this[Count - 1];
            }
        }

        public new T this[int index]
        {
            get => _array[index];
            set => _array[index] = value;
        }

        public new void Add(T node)
        {
            if (Count == 0)
            {
                //(this as List<T>).Add(obj);
                base.Add(node);
                Last = node;
                FillArray();
                return;
            }

            Last.Next = node;
            node.Previous = Last;
            //(this as List<T>).Add(obj);
            base.Add(node);

            //First = this[0];
            Last = node;
            FillArray();
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

        private void FillArray()
        {
            if (Count > 0)
            {
                _array = new T[Count];
                CopyTo(_array);
            }
            else
            {
                _array = null;
            }
        }
    }

    unsafe public class ListXNode<T>
    {
        public T Next;
        public T Previous;
    }
}
