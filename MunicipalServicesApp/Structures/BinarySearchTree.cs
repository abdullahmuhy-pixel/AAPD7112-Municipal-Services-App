using System.Collections.Generic;

namespace MunicipalServicesApp.Structures
{
    /// <summary>
    /// A plain (unbalanced) binary search tree. Every node has at most two
    /// children: smaller keys go to the left and larger keys go to the right.
    ///
    /// It is kept in the application as the baseline to compare against the
    /// self-balancing trees. If keys arrive already sorted (like request IDs
    /// SR-1001, SR-1002, ...) this tree degrades into a long chain and
    /// lookups become as slow as scanning a list.
    /// </summary>
    public class BinarySearchTree<TKey, TValue> : ITreeIndex<TKey, TValue>
    {
        private class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Left;
            public Node Right;

            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private readonly IComparer<TKey> _comparer;
        private Node _root;

        public BinarySearchTree(IComparer<TKey> comparer = null)
        {
            _comparer = comparer ?? Comparer<TKey>.Default;
        }

        public string Name { get { return "Binary Search Tree"; } }
        public int Count { get; private set; }
        public int Height { get { return HeightOf(_root); } }

        public void Insert(TKey key, TValue value)
        {
            if (_root == null)
            {
                _root = new Node(key, value);
                Count = 1;
                return;
            }

            Node current = _root;
            while (true)
            {
                int cmp = _comparer.Compare(key, current.Key);
                if (cmp == 0)
                {
                    current.Value = value; // same key: replace the stored value
                    return;
                }

                if (cmp < 0)
                {
                    if (current.Left == null)
                    {
                        current.Left = new Node(key, value);
                        Count++;
                        return;
                    }
                    current = current.Left;
                }
                else
                {
                    if (current.Right == null)
                    {
                        current.Right = new Node(key, value);
                        Count++;
                        return;
                    }
                    current = current.Right;
                }
            }
        }

        public bool TryFind(TKey key, out TValue value, out int comparisons)
        {
            comparisons = 0;
            Node current = _root;
            while (current != null)
            {
                comparisons++;
                int cmp = _comparer.Compare(key, current.Key);
                if (cmp == 0)
                {
                    value = current.Value;
                    return true;
                }
                current = cmp < 0 ? current.Left : current.Right;
            }

            value = default(TValue);
            return false;
        }

        /// <summary>Left, node, right: gives the values in sorted key order.</summary>
        public List<TValue> InOrder()
        {
            var result = new List<TValue>();
            WalkInOrder(_root, result);
            return result;
        }

        /// <summary>Node, left, right.</summary>
        public List<TValue> PreOrder()
        {
            var result = new List<TValue>();
            WalkPreOrder(_root, result);
            return result;
        }

        /// <summary>Left, right, node.</summary>
        public List<TValue> PostOrder()
        {
            var result = new List<TValue>();
            WalkPostOrder(_root, result);
            return result;
        }

        private static void WalkInOrder(Node n, List<TValue> output)
        {
            if (n == null) return;
            WalkInOrder(n.Left, output);
            output.Add(n.Value);
            WalkInOrder(n.Right, output);
        }

        private static void WalkPreOrder(Node n, List<TValue> output)
        {
            if (n == null) return;
            output.Add(n.Value);
            WalkPreOrder(n.Left, output);
            WalkPreOrder(n.Right, output);
        }

        private static void WalkPostOrder(Node n, List<TValue> output)
        {
            if (n == null) return;
            WalkPostOrder(n.Left, output);
            WalkPostOrder(n.Right, output);
            output.Add(n.Value);
        }

        private static int HeightOf(Node n)
        {
            if (n == null) return 0;
            int left = HeightOf(n.Left);
            int right = HeightOf(n.Right);
            return 1 + (left > right ? left : right);
        }
    }
}
