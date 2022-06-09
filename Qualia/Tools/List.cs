﻿using System;
using System.Collections.Generic;

namespace Tools
{
    public class ListX<T> : List<T> where T : ListXNode<T>
    {
        //public T First { get; private set; }
        public T Last { get; private set; }

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
    }

    unsafe public class ListXNode<T>
    {
        public T Next;
        public T Previous;
    }
}
