using Qualia;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tools
{
    public class ListX<T> : List<T> where T : ListXNode<T>
    {
        public T Last { get; private set; }

        private T[] _array = Array.Empty<T>();

        public ListX()
        {
            //
        }

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

        public void ForEach(Action<T> action)
        {
            Array.ForEach(_array, action);
        }

        internal List<T> Where(Predicate<T> match)
        {
            var list = new List<T>(_array);
            return list.FindAll(match);
        }

        public int Count => _array.Length;

        public T this[int index]
        {
            get
            {
                return _array.Length > 0 ? _array[index] : null;
            }
            
            set
            {
                _array[index] = value;
            }
        }

        public void Add(T node)
        {
            var array = new T[Count + 1];
            _array.CopyTo(array, 0);
            array[Count] = node;
            _array = array;

            if (Count == 1)
            {
                Last = node;
                return;
            }

            Last.Next = node;
            node.Previous = Last;
            Last = node;
        }

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

        internal bool Any()
        {
            Any();
            return _array.Length > 0;
        }

        internal T FirstOrDefault(Predicate<T> match)
        {
            var list = new List<T>(_array);
            return list.Find(match);
        }

        internal bool Any(Predicate<T> match)
        {
            var list = new List<T>(_array);
            return list.Exists(match);
        }

        internal int CountIf(Predicate<T> match)
        {
            var list = new List<T>(_array);
            return Where(match).Count;
        }

        internal float Max(Func<T, float> value)
        {
            var list = new List<T>(_array);
            return Enumerable.Max<T>(list, value);
        }

        internal T Find(Predicate<T> match)
        {
            return Array.Find(_array, match);
        }
    }

    unsafe public class ListXNode<T>
    {
        public T Next;
        public T Previous;
    }
}
