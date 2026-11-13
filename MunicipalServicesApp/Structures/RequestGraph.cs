using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalServicesApp.Structures
{
    public class GraphEdge
    {
        public string From { get; }
        public string To { get; }
        public double Weight { get; }

        public GraphEdge(string from, string to, double weight)
        {
            From = from;
            To = to;
            Weight = weight;
        }
    }

    public class MstResult
    {
        public List<GraphEdge> Edges { get; }
        public double TotalWeight { get; }
        public int VertexCount { get; }

        /// <summary>1 when everything is connected; more than 1 means a spanning forest.</summary>
        public int Components { get; }

        public MstResult(List<GraphEdge> edges, double totalWeight, int vertexCount, int components)
        {
            Edges = edges;
            TotalWeight = totalWeight;
            VertexCount = vertexCount;
            Components = components;
        }
    }

    /// <summary>
    /// An undirected, weighted graph stored as an adjacency list (a
    /// dictionary from each vertex to the edges touching it).
    ///
    /// Vertices are service request IDs. An edge means two requests are
    /// related (same category or physically close) and its weight is the
    /// distance between them in km.
    /// </summary>
    public class RequestGraph
    {
        private readonly Dictionary<string, List<GraphEdge>> _adjacency
            = new Dictionary<string, List<GraphEdge>>(StringComparer.Ordinal);

        private readonly List<GraphEdge> _edges = new List<GraphEdge>();

        public int VertexCount { get { return _adjacency.Count; } }
        public int EdgeCount { get { return _edges.Count; } }
        public IReadOnlyList<GraphEdge> Edges { get { return _edges; } }

        public void AddVertex(string id)
        {
            if (!_adjacency.ContainsKey(id))
                _adjacency[id] = new List<GraphEdge>();
        }

        public void AddEdge(string a, string b, double weight)
        {
            if (a == b) return;

            AddVertex(a);
            AddVertex(b);
            if (HasEdge(a, b)) return;

            var edge = new GraphEdge(a, b, weight);
            _edges.Add(edge);
            _adjacency[a].Add(edge);
            _adjacency[b].Add(edge);
        }

        public bool HasEdge(string a, string b)
        {
            if (!_adjacency.ContainsKey(a)) return false;
            return _adjacency[a].Any(e => Other(e, a) == b);
        }

        public bool Contains(string id)
        {
            return _adjacency.ContainsKey(id);
        }

        /// <summary>
        /// Breadth first search: visits the start vertex, then everything one
        /// edge away, then two edges away, and so on, using a queue. The
        /// depth is the number of "hops" from the start.
        /// </summary>
        public List<(string Id, int Depth)> BreadthFirst(string start, int maxDepth = int.MaxValue)
        {
            var result = new List<(string Id, int Depth)>();
            if (!_adjacency.ContainsKey(start)) return result;

            var visited = new HashSet<string>(StringComparer.Ordinal) { start };
            var queue = new Queue<(string Id, int Depth)>();
            queue.Enqueue((start, 0));

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                result.Add(current);

                if (current.Depth >= maxDepth) continue;

                foreach (var edge in _adjacency[current.Id].OrderBy(e => e.Weight))
                {
                    string neighbour = Other(edge, current.Id);
                    if (visited.Add(neighbour))
                        queue.Enqueue((neighbour, current.Depth + 1));
                }
            }

            return result;
        }

        /// <summary>
        /// Depth first search: follows one chain of related requests as far
        /// as it goes (always taking the closest unvisited neighbour first)
        /// before backing up and trying the next branch.
        /// </summary>
        public List<string> DepthFirst(string start)
        {
            var order = new List<string>();
            if (!_adjacency.ContainsKey(start)) return order;

            var visited = new HashSet<string>(StringComparer.Ordinal);
            VisitDepthFirst(start, visited, order);
            return order;
        }

        private void VisitDepthFirst(string vertex, HashSet<string> visited, List<string> order)
        {
            visited.Add(vertex);
            order.Add(vertex);

            foreach (var edge in _adjacency[vertex].OrderBy(e => e.Weight))
            {
                string neighbour = Other(edge, vertex);
                if (!visited.Contains(neighbour))
                    VisitDepthFirst(neighbour, visited, order);
            }
        }

        /// <summary>Number of separate groups of connected vertices (optionally only counting some vertices).</summary>
        public int CountComponents(Func<string, bool> include = null)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            int components = 0;

            foreach (var vertex in _adjacency.Keys)
            {
                if (include != null && !include(vertex)) continue;
                if (visited.Contains(vertex)) continue;

                components++;
                var queue = new Queue<string>();
                queue.Enqueue(vertex);
                visited.Add(vertex);

                while (queue.Count > 0)
                {
                    string current = queue.Dequeue();
                    foreach (var edge in _adjacency[current])
                    {
                        string neighbour = Other(edge, current);
                        if (include != null && !include(neighbour)) continue;
                        if (visited.Add(neighbour))
                            queue.Enqueue(neighbour);
                    }
                }
            }

            return components;
        }

        /// <summary>
        /// Kruskal's minimum spanning tree. Edges are sorted from shortest to
        /// longest and added one by one, skipping any edge that would form a
        /// loop (checked with a union-find structure). The result is the
        /// cheapest set of links that still connects every vertex.
        /// If the graph is not fully connected the result is a forest.
        /// </summary>
        public MstResult MinimumSpanningTree(Func<string, bool> include = null)
        {
            var vertices = _adjacency.Keys.Where(v => include == null || include(v)).ToList();

            var parent = new Dictionary<string, string>(StringComparer.Ordinal);
            var rank = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var v in vertices)
            {
                parent[v] = v;
                rank[v] = 0;
            }

            var candidates = _edges
                .Where(e => parent.ContainsKey(e.From) && parent.ContainsKey(e.To))
                .OrderBy(e => e.Weight)
                .ThenBy(e => e.From, StringComparer.Ordinal)
                .ThenBy(e => e.To, StringComparer.Ordinal)
                .ToList();

            var chosen = new List<GraphEdge>();
            double total = 0;

            foreach (var edge in candidates)
            {
                string rootA = Find(parent, edge.From);
                string rootB = Find(parent, edge.To);
                if (rootA == rootB) continue; // would create a cycle

                Union(parent, rank, rootA, rootB);
                chosen.Add(edge);
                total += edge.Weight;
            }

            int components = vertices.Count - chosen.Count;
            return new MstResult(chosen, total, vertices.Count, components);
        }

        private static string Find(Dictionary<string, string> parent, string v)
        {
            string root = v;
            while (parent[root] != root)
                root = parent[root];

            // Path compression: point everything on the path straight at the root.
            while (parent[v] != root)
            {
                string next = parent[v];
                parent[v] = root;
                v = next;
            }

            return root;
        }

        private static void Union(Dictionary<string, string> parent, Dictionary<string, int> rank, string a, string b)
        {
            if (rank[a] < rank[b])
            {
                parent[a] = b;
            }
            else if (rank[a] > rank[b])
            {
                parent[b] = a;
            }
            else
            {
                parent[b] = a;
                rank[a]++;
            }
        }

        private static string Other(GraphEdge edge, string vertex)
        {
            return edge.From == vertex ? edge.To : edge.From;
        }
    }
}
