using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Structures
{
    /// <summary>
    /// A binary min-heap stored in a List. The smallest item (according to
    /// the comparer) is always at index 0. For the item at index i, its
    /// children live at 2i+1 and 2i+2 and its parent at (i-1)/2.
    ///
    /// Inserting sifts the new item up, and extracting the minimum moves the
    /// last item to the root and sifts it down, so both take O(log n).
    ///
    /// In the application it orders waiting service requests so the most
    /// urgent (lowest priority number, then oldest) is always ready first.
    /// </summary>
    public class BinaryHeap<T>
    {
        private readonly List<T> _items = new List<T>();
        private readonly IComparer<T> _comparer;

        public BinaryHeap(IComparer<T> comparer = null)
        {
            _comparer = comparer ?? Comparer<T>.Default;
        }

        public int Count { get { return _items.Count; } }

        public void Insert(T item)
        {
            _items.Add(item);
            SiftUp(_items.Count - 1);
        }

        public T Peek()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("The heap is empty.");
            return _items[0];
        }

        public T ExtractMin()
        {
            if (_items.Count == 0)
                throw new InvalidOperationException("The heap is empty.");

            T top = _items[0];
            int last = _items.Count - 1;
            _items[0] = _items[last];
            _items.RemoveAt(last);

            if (_items.Count > 0)
                SiftDown(0);

            return top;
        }

        public bool TryExtractMin(out T item)
        {
            if (_items.Count == 0)
            {
                item = default(T);
                return false;
            }

            item = ExtractMin();
            return true;
        }

        /// <summary>
        /// Returns every item in priority order without changing the heap
        /// (it works on a copy), so the screen can show the queue repeatedly.
        /// </summary>
        public List<T> ToSortedList()
        {
            var copy = new BinaryHeap<T>(_comparer);
            copy._items.AddRange(_items);

            var result = new List<T>();
            while (copy.Count > 0)
                result.Add(copy.ExtractMin());
            return result;
        }

        private void SiftUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (_comparer.Compare(_items[index], _items[parent]) >= 0) break;

                Swap(index, parent);
                index = parent;
            }
        }

        private void SiftDown(int index)
        {
            int n = _items.Count;
            while (true)
            {
                int left = 2 * index + 1;
                int right = 2 * index + 2;
                int smallest = index;

                if (left < n && _comparer.Compare(_items[left], _items[smallest]) < 0)
                    smallest = left;
                if (right < n && _comparer.Compare(_items[right], _items[smallest]) < 0)
                    smallest = right;

                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            T temp = _items[i];
            _items[i] = _items[j];
            _items[j] = temp;
        }
    }
}
