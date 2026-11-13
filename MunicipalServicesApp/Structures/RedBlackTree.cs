using System.Collections.Generic;

namespace MunicipalServicesApp.Structures
{
    /// <summary>
    /// A red-black tree (left-leaning variant). Every node is coloured red or
    /// black and the tree follows these rules, which keep it balanced:
    ///   1. The root is black.
    ///   2. A red node never has a red child.
    ///   3. Every path from a node down to an empty spot passes through the
    ///      same number of black nodes (the "black height").
    ///
    /// Balance is restored after an insert using rotations and colour flips.
    /// Compared with an AVL tree it does fewer rotations on insert, which
    /// makes it a good fit for data that keeps arriving, like new requests
    /// ordered by the date they were submitted.
    ///
    /// Only insert and search are implemented because requests are never
    /// deleted in this application (they just move to a Resolved status).
    /// </summary>
    public class RedBlackTree<TKey, TValue> : ITreeIndex<TKey, TValue>
    {
        private class Node
        {
            public TKey Key;
            public TValue Value;
            public Node Left;
            public Node Right;
            public bool Red;

            public Node(TKey key, TValue value, bool red)
            {
                Key = key;
                Value = value;
                Red = red;
            }
        }

        private readonly IComparer<TKey> _comparer;
        private Node _root;

        public RedBlackTree(IComparer<TKey> comparer = null)
        {
            _comparer = comparer ?? Comparer<TKey>.Default;
        }

        public string Name { get { return "Red-Black Tree"; } }
        public int Count { get; private set; }
        public int Height { get { return HeightOf(_root); } }

        public void Insert(TKey key, TValue value)
        {
            _root = InsertNode(_root, key, value);
            _root.Red = false; // rule 1: the root is always black
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

        /// <summary>Checks all three red-black rules. Used to prove the tree is valid.</summary>
        public bool IsValid()
        {
            if (_root == null) return true;
            if (_root.Red) return false;
            return CheckBlackHeight(_root) >= 0;
        }

        /// <summary>Number of black nodes on any root-to-leaf path (-1 if the tree is invalid).</summary>
        public int BlackHeight
        {
            get
            {
                if (_root == null) return 0;
                int h = CheckBlackHeight(_root);
                return h < 0 ? -1 : h - 1;
            }
        }

        private Node InsertNode(Node h, TKey key, TValue value)
        {
            if (h == null)
            {
                Count++;
                return new Node(key, value, true); // new nodes always start red
            }

            int cmp = _comparer.Compare(key, h.Key);
            if (cmp < 0)
            {
                h.Left = InsertNode(h.Left, key, value);
            }
            else if (cmp > 0)
            {
                h.Right = InsertNode(h.Right, key, value);
            }
            else
            {
                h.Value = value;
            }

            // Repair on the way back up.
            if (IsRed(h.Right) && !IsRed(h.Left)) h = RotateLeft(h);
            if (IsRed(h.Left) && IsRed(h.Left.Left)) h = RotateRight(h);
            if (IsRed(h.Left) && IsRed(h.Right)) FlipColours(h);

            return h;
        }

        private static bool IsRed(Node n)
        {
            return n != null && n.Red;
        }

        private static Node RotateLeft(Node h)
        {
            Node x = h.Right;
            h.Right = x.Left;
            x.Left = h;
            x.Red = h.Red;
            h.Red = true;
            return x;
        }

        private static Node RotateRight(Node h)
        {
            Node x = h.Left;
            h.Left = x.Right;
            x.Right = h;
            x.Red = h.Red;
            h.Red = true;
            return x;
        }

        private static void FlipColours(Node h)
        {
            // Both children are red, so push the red up one level.
            h.Red = true;
            h.Left.Red = false;
            h.Right.Red = false;
        }

        // Returns the black height (counting the empty leaf as 1), or -1 if a rule is broken.
        private static int CheckBlackHeight(Node n)
        {
            if (n == null) return 1;
            if (n.Red && (IsRed(n.Left) || IsRed(n.Right))) return -1;

            int left = CheckBlackHeight(n.Left);
            int right = CheckBlackHeight(n.Right);
            if (left < 0 || right < 0 || left != right) return -1;

            return left + (n.Red ? 0 : 1);
        }

        private static int HeightOf(Node n)
        {
            if (n == null) return 0;
            int left = HeightOf(n.Left);
            int right = HeightOf(n.Right);
            return 1 + (left > right ? left : right);
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
