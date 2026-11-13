using System.Collections.Generic;

namespace MunicipalServicesApp.Structures
{
    /// <summary>
    /// A self-balancing binary search tree (AVL tree). After every insert it
    /// checks the "balance factor" (height of left subtree minus height of
    /// right subtree) of each node on the way back up. If a node becomes
    /// lopsided (factor below -1 or above 1) it is fixed with a single or
    /// double rotation, so the tree height always stays close to log2(n).
    ///
    /// In the application this is the main index used to track a service
    /// request by its unique ID, because lookups are guaranteed to be fast.
    /// </summary>
    public class AvlTree<TKey, TValue> : ITreeIndex<TKey, TValue>
    {
        private class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Left;
            public Node Right;
            public int Height = 1;

            public Node(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private readonly IComparer<TKey> _comparer;
        private Node _root;

        public AvlTree(IComparer<TKey> comparer = null)
        {
            _comparer = comparer ?? Comparer<TKey>.Default;
        }

        public string Name { get { return "AVL Tree"; } }
        public int Count { get; private set; }
        public int Height { get { return HeightOf(_root); } }

        /// <summary>How many rotations have been needed so far to keep the tree balanced.</summary>
        public int Rotations { get; private set; }

        public void Insert(TKey key, TValue value)
        {
            _root = InsertNode(_root, key, value);
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

        public List<TValue> InOrder()
        {
            var result = new List<TValue>();
            WalkInOrder(_root, result);
            return result;
        }

        private Node InsertNode(Node node, TKey key, TValue value)
        {
            if (node == null)
            {
                Count++;
                return new Node(key, value);
            }

            int cmp = _comparer.Compare(key, node.Key);
            if (cmp < 0)
            {
                node.Left = InsertNode(node.Left, key, value);
            }
            else if (cmp > 0)
            {
                node.Right = InsertNode(node.Right, key, value);
            }
            else
            {
                node.Value = value;
                return node;
            }

            return Rebalance(node);
        }

        private Node Rebalance(Node node)
        {
            UpdateHeight(node);
            int balance = BalanceFactor(node);

            if (balance > 1)
            {
                // Left heavy. If the left child leans right, rotate it left first (left-right case).
                if (BalanceFactor(node.Left) < 0)
                {
                    node.Left = RotateLeft(node.Left);
                }
                return RotateRight(node);
            }

            if (balance < -1)
            {
                // Right heavy. If the right child leans left, rotate it right first (right-left case).
                if (BalanceFactor(node.Right) > 0)
                {
                    node.Right = RotateRight(node.Right);
                }
                return RotateLeft(node);
            }

            return node;
        }

        private Node RotateRight(Node y)
        {
            Node x = y.Left;
            Node middle = x.Right;

            x.Right = y;
            y.Left = middle;

            UpdateHeight(y);
            UpdateHeight(x);
            Rotations++;
            return x;
        }

        private Node RotateLeft(Node x)
        {
            Node y = x.Right;
            Node middle = y.Left;

            y.Left = x;
            x.Right = middle;

            UpdateHeight(x);
            UpdateHeight(y);
            Rotations++;
            return y;
        }

        private static int HeightOf(Node n)
        {
            return n == null ? 0 : n.Height;
        }

        private static void UpdateHeight(Node n)
        {
            int left = HeightOf(n.Left);
            int right = HeightOf(n.Right);
            n.Height = 1 + (left > right ? left : right);
        }

        private static int BalanceFactor(Node n)
        {
            return n == null ? 0 : HeightOf(n.Left) - HeightOf(n.Right);
        }

        private static void WalkInOrder(Node n, List<TValue> output)
        {
            if (n == null) return;
            WalkInOrder(n.Left, output);
            output.Add(n.Value);
            WalkInOrder(n.Right, output);
        }
    }
}
