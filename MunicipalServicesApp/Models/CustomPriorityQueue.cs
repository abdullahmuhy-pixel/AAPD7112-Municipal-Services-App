using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// A minimal binary-heap based priority queue.
    ///
    /// .NET Framework 4.7.2 does not ship System.Collections.Generic.PriorityQueue
    /// (that type was only added in .NET 6), so it is implemented here from
    /// scratch to satisfy the Technical Requirement to implement stacks,
    /// queues, or priority queues for managing event-related data.
    ///
    /// Lower priority values are dequeued first (a min-heap). In this
    /// application priority is the number of days until an event occurs, so
    /// the soonest / most urgent event is always at the front of the queue.
    /// </summary>
    public class CustomPriorityQueue<T>
    {
        private readonly List<(T Item, double Priority)> _heap = new List<(T, double)>();

        public int Count => _heap.Count;

        public void Enqueue(T item, double priority)
        {
            _heap.Add((item, priority));
            int child = _heap.Count - 1;

            // Bubble the new item up until the heap property is restored.
            while (child > 0)
            {
                int parent = (child - 1) / 2;
                if (_heap[child].Priority >= _heap[parent].Priority) break;

                Swap(child, parent);
                child = parent;
            }
        }

        public T Dequeue()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("The priority queue is empty.");

            T result = _heap[0].Item;
            int last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);

            // Bubble the root down until the heap property is restored.
            int parent = 0;
            while (true)
            {
                int left = parent * 2 + 1;
                int right = parent * 2 + 2;
                int smallest = parent;

                if (left < _heap.Count && _heap[left].Priority < _heap[smallest].Priority)
                    smallest = left;
                if (right < _heap.Count && _heap[right].Priority < _heap[smallest].Priority)
                    smallest = right;

                if (smallest == parent) break;

                Swap(parent, smallest);
                parent = smallest;
            }

            return result;
        }

        public T Peek()
        {
            if (_heap.Count == 0)
                throw new InvalidOperationException("The priority queue is empty.");
            return _heap[0].Item;
        }

        /// <summary>
        /// Returns the items in priority order without removing them from
        /// the queue, so the UI can display the current ordering repeatedly.
        /// </summary>
        public List<T> ToOrderedList()
        {
            var copy = new CustomPriorityQueue<T>();
            copy._heap.AddRange(_heap);
            var result = new List<T>();
            while (copy.Count > 0)
                result.Add(copy.Dequeue());
            return result;
        }

        private void Swap(int i, int j)
        {
            var temp = _heap[i];
            _heap[i] = _heap[j];
            _heap[j] = temp;
        }
    }
}
