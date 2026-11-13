using System.Collections.Generic;

namespace MunicipalServicesApp.Structures
{
    /// <summary>
    /// Common shape shared by the three search trees (BST, AVL, Red-Black)
    /// so the Service Request Status feature can compare them side by side.
    /// </summary>
    public interface ITreeIndex<TKey, TValue>
    {
        string Name { get; }
        int Count { get; }

        /// <summary>Number of nodes on the longest root-to-leaf path (0 for an empty tree).</summary>
        int Height { get; }

        void Insert(TKey key, TValue value);

        /// <summary>
        /// Looks a key up and reports how many key comparisons were needed,
        /// which is how the efficiency of each tree is demonstrated.
        /// </summary>
        bool TryFind(TKey key, out TValue value, out int comparisons);

        /// <summary>All values in ascending key order.</summary>
        List<TValue> InOrder();
    }
}
