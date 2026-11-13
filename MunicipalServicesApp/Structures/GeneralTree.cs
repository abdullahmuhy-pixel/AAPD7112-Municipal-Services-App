using System.Collections.Generic;

namespace MunicipalServicesApp.Structures
{
    /// <summary>
    /// A node in a basic (general) tree. Unlike a binary tree, a node can
    /// have any number of children. It is used to show the hierarchy:
    /// Municipality, then Category, then individual requests.
    /// </summary>
    public class GeneralTreeNode<T>
    {
        public T Value { get; set; }
        public GeneralTreeNode<T> Parent { get; private set; }
        public List<GeneralTreeNode<T>> Children { get; } = new List<GeneralTreeNode<T>>();

        public GeneralTreeNode(T value)
        {
            Value = value;
        }

        public GeneralTreeNode<T> AddChild(T value)
        {
            var child = new GeneralTreeNode<T>(value);
            child.Parent = this;
            Children.Add(child);
            return child;
        }

        public bool IsLeaf { get { return Children.Count == 0; } }

        /// <summary>Total number of nodes in the subtree rooted here (including this node).</summary>
        public int CountNodes()
        {
            int total = 1;
            foreach (var child in Children)
                total += child.CountNodes();
            return total;
        }

        /// <summary>Number of levels in the subtree rooted here (a lone node is 1).</summary>
        public int Height()
        {
            int tallest = 0;
            foreach (var child in Children)
            {
                int h = child.Height();
                if (h > tallest) tallest = h;
            }
            return 1 + tallest;
        }

        /// <summary>Depth first, parent before children.</summary>
        public List<T> PreOrder()
        {
            var result = new List<T>();
            Visit(this, result);
            return result;
        }

        private static void Visit(GeneralTreeNode<T> node, List<T> output)
        {
            output.Add(node.Value);
            foreach (var child in node.Children)
                Visit(child, output);
        }
    }
}
