using System;

namespace Qualia.Tools
{
    sealed public class ListX<T> where T : ListXNode<T>
    {
        public T First;
        public T Last;

        private T[] _array = Array.Empty<T>();

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

        public T FirstOrDefault(Predicate<T> match)
        {
            return Array.Find(_array, match);
        }

        public bool Any()
        {
            return First != null;
        }

        public bool Any(Predicate<T> match)
        {
            return Array.Find(_array, match) != null;
        }

        public int IndexOf(T node)
        {
            return Array.IndexOf(_array, node);
        }

        public void Replace(int index, T node)
        {
            var prevNode = _array[index];
            _array[index] = node;
            node.Next = prevNode.Next;
            node.Previous = prevNode.Previous;

            if (node.Previous != null)
            {
                node.Previous.Next = node;
            }

            if (node.Next != null)
            {
                node.Next.Previous = node;
            }

            First = _array[0];
            Last = _array[_array.Length - 1];
        }

        public int CountIf(Func<T, bool> value)
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

        public float Max(Func<T, float> value)
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

        public void ForEach(Action<T> action)
        {
            Array.ForEach(_array, action);
        }

        public T Find(Predicate<T> match)
        {
            return Array.Find(_array, match);
        }
    }

    public class ListXNode<T> where T : class
    {
        public T Next;
        public T Previous;

        public void ForEach(Action<T> action)
        {
            T current = this as T;
            while (current != null)
            {
                action(current);
                current = (current as ListXNode<T>).Next;
            }
        }
    }
}
