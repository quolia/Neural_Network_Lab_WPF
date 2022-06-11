using System;

namespace Tools
{
    sealed public class ListX<T> where T : ListXNode<T>
    {
        public T First;
        public T Last;

        private T[] _array = Array.Empty<T>();
        private int _removeCount;

        public ListX(int capacity)
        {
            //
        }

        public T this[int index] => _array[index];

        public int Count => _array.Length;

        public void Add(T node)
        {
            if (Last != null)
            { 
                Last.Next = node;
                node.Previous = Last;
            }

            Array.Resize(ref _array, Count + 1);
            _array[Count - 1] = node;

            First = _array[0];
            Last = node;
        }

        internal void Remove(T node)
        {
            if (node.Previous != null)
            {
                node.Previous.Next = node.Next;
            }

            if (node.Next != null)
            {
                node.Next.Previous = node.Previous;
            }

            ++_removeCount;
        }

        internal void CommitRemove()
        {
            if (_removeCount == 0)
            {
                return;
            }

            var node = First;
            Array.Resize(ref _array, Count - _removeCount);
            
            for (int i = 0; i < Count; ++i)
            {
                _array[i] = node;
                node = node.Next;
            }

            _removeCount = 0;
        }

        internal T FirstOrDefault(Predicate<T> match)
        {
            return Array.Find(_array, match);
        }

        internal bool Any()
        {
            return First != null;
        }

        internal bool Any(Predicate<T> match)
        {
            return Array.Find(_array, match) != null;
        }

        internal int CountIf(Func<T, bool> value)
        {
            int count = 0;

            var node = First;

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

            var node = First;
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

        internal void ForEach(Action<T> action)
        {
            Array.ForEach(_array, action);
        }

        internal T Find(Predicate<T> match)
        {
            return Array.Find(_array, match);
        }
    }

    public class ListXNode<T>
    {
        public T Next;
        public T Previous;
    }
}
