using System;
using System.Collections.Generic;
using System.Linq;
using MunicipalServicesApp.Structures;

namespace MunicipalServicesApp.Models
{
    /// <summary>Sort key for the red-black tree: submission date first, then ID to keep keys unique.</summary>
    public struct SubmissionKey : IComparable<SubmissionKey>
    {
        public DateTime Date { get; }
        public string Id { get; }

        public SubmissionKey(DateTime date, string id)
        {
            Date = date;
            Id = id;
        }

        public int CompareTo(SubmissionKey other)
        {
            int byDate = Date.CompareTo(other.Date);
            return byDate != 0 ? byDate : string.CompareOrdinal(Id, other.Id);
        }
    }

    /// <summary>
    /// Central in-memory store for service requests. The same requests are
    /// held in several structures at once, each chosen for a specific job:
    ///
    ///   - AvlTree (key: request ID): main lookup when a citizen tracks a request.
    ///   - BinarySearchTree (key: request ID): unbalanced baseline, used to show
    ///     how many extra comparisons a plain BST needs.
    ///   - RedBlackTree (key: date submitted): requests kept in date order.
    ///   - BinaryHeap (min-heap): the urgent queue, most critical first.
    ///   - RequestGraph: relationships between requests (BFS, DFS, MST).
    ///   - GeneralTreeNode: Municipality, Category, Request hierarchy.
    /// </summary>
    public static class ServiceRequestRepository
    {
        /// <summary>Two requests are linked in the graph if they share a category or are this close (km).</summary>
        public const double LinkDistanceKm = 4.0;

        private static readonly AvlTree<string, ServiceRequest> _avlById
            = new AvlTree<string, ServiceRequest>(StringComparer.Ordinal);

        private static readonly BinarySearchTree<string, ServiceRequest> _bstById
            = new BinarySearchTree<string, ServiceRequest>(StringComparer.Ordinal);

        private static readonly RedBlackTree<SubmissionKey, ServiceRequest> _rbByDate
            = new RedBlackTree<SubmissionKey, ServiceRequest>();

        private static readonly BinaryHeap<ServiceRequest> _urgentHeap
            = new BinaryHeap<ServiceRequest>(Comparer<ServiceRequest>.Create(CompareUrgency));

        private static RequestGraph _graph = new RequestGraph();

        public static AvlTree<string, ServiceRequest> AvlIndex { get { return _avlById; } }
        public static BinarySearchTree<string, ServiceRequest> BstIndex { get { return _bstById; } }
        public static RedBlackTree<SubmissionKey, ServiceRequest> DateIndex { get { return _rbByDate; } }
        public static RequestGraph Graph { get { return _graph; } }
        public static int Count { get { return _avlById.Count; } }

        static ServiceRequestRepository()
        {
            Seed();
        }

        // ---------- adding ----------

        public static void Add(ServiceRequest request)
        {
            if (request == null) return;
            AddCore(request);
            RebuildGraph();
        }

        /// <summary>
        /// Called by the Report Issues feature (Part 1) so every submitted
        /// issue can be tracked here using the same reference number.
        /// </summary>
        public static void AddFromIssue(Issue issue)
        {
            if (issue == null) return;

            double x, y;
            PseudoCoordinates(issue.Location, out x, out y);

            var request = new ServiceRequest(
                issue.ReferenceNumber,
                CategoryText(issue.Category),
                issue.Location,
                issue.Description,
                DefaultPriority(issue.Category),
                issue.DateReported,
                x, y,
                issue.AttachmentPath);

            Add(request);
        }

        private static void AddCore(ServiceRequest request)
        {
            _avlById.Insert(request.Id, request);
            _bstById.Insert(request.Id, request);
            _rbByDate.Insert(new SubmissionKey(request.DateSubmitted, request.Id), request);

            if (request.Status == RequestStatus.Submitted)
                _urgentHeap.Insert(request);
        }

        // ---------- lookups ----------

        public static ServiceRequest Find(string id)
        {
            ServiceRequest request;
            int comparisons;
            return _avlById.TryFind(id, out request, out comparisons) ? request : null;
        }

        /// <summary>Looks the ID up in both trees so the screen can compare their efficiency.</summary>
        public static bool TryFind(string id, out ServiceRequest request, out int avlComparisons, out int bstComparisons)
        {
            ServiceRequest fromBst;
            _bstById.TryFind(id, out fromBst, out bstComparisons);
            return _avlById.TryFind(id, out request, out avlComparisons);
        }

        /// <summary>All requests, oldest first (red-black tree in-order walk).</summary>
        public static List<ServiceRequest> AllByDate()
        {
            return _rbByDate.InOrder();
        }

        // ---------- heap ----------

        /// <summary>Requests still waiting to be picked up, most urgent first.</summary>
        public static List<ServiceRequest> UrgentQueue()
        {
            return _urgentHeap.ToSortedList()
                .Where(r => r.Status == RequestStatus.Submitted)
                .ToList();
        }

        /// <summary>
        /// Takes the most urgent waiting request off the heap and assigns it.
        /// Entries that were already moved on by hand are skipped (lazy removal).
        /// </summary>
        public static ServiceRequest ProcessNext()
        {
            ServiceRequest next;
            while (_urgentHeap.TryExtractMin(out next))
            {
                if (next.Status != RequestStatus.Submitted) continue;

                next.AdvanceStatus();
                return next;
            }

            return null;
        }

        private static int CompareUrgency(ServiceRequest a, ServiceRequest b)
        {
            int byPriority = a.Priority.CompareTo(b.Priority);
            if (byPriority != 0) return byPriority;

            int byDate = a.DateSubmitted.CompareTo(b.DateSubmitted);
            if (byDate != 0) return byDate;

            return string.CompareOrdinal(a.Id, b.Id);
        }

        // ---------- graph ----------

        private static void RebuildGraph()
        {
            var graph = new RequestGraph();
            var all = _rbByDate.InOrder();

            foreach (var request in all)
                graph.AddVertex(request.Id);

            for (int i = 0; i < all.Count; i++)
            {
                for (int j = i + 1; j < all.Count; j++)
                {
                    double distance = all[i].DistanceTo(all[j]);
                    bool sameCategory = all[i].Category == all[j].Category;
                    if (sameCategory || distance <= LinkDistanceKm)
                        graph.AddEdge(all[i].Id, all[j].Id, Math.Round(distance, 2));
                }
            }

            _graph = graph;
        }

        /// <summary>Requests reachable from the given one within maxDepth hops (not including itself).</summary>
        public static List<(string Id, int Depth)> Related(string id, int maxDepth)
        {
            return _graph.BreadthFirst(id, maxDepth).Where(x => x.Id != id).ToList();
        }

        public static bool IsOpen(string id)
        {
            ServiceRequest request = Find(id);
            return request != null && request.Status != RequestStatus.Resolved;
        }

        // ---------- general tree ----------

        public static GeneralTreeNode<string> BuildCategoryTree()
        {
            var root = new GeneralTreeNode<string>("Municipal Service Requests (" + Count + ")");

            var groups = _rbByDate.InOrder()
                .GroupBy(r => r.Category)
                .OrderBy(g => g.Key, StringComparer.Ordinal);

            foreach (var group in groups)
            {
                var categoryNode = root.AddChild(group.Key + " (" + group.Count() + ")");
                foreach (var request in group)
                {
                    categoryNode.AddChild(request.Id + "  |  " + request.Location + "  |  " + request.StatusText);
                }
            }

            return root;
        }

        // ---------- helpers ----------

        public static string CategoryText(IssueCategory category)
        {
            switch (category)
            {
                case IssueCategory.WaterAndSewage: return "Water and Sewage";
                default: return category.ToString();
            }
        }

        private static int DefaultPriority(IssueCategory category)
        {
            switch (category)
            {
                case IssueCategory.Electricity:
                case IssueCategory.WaterAndSewage:
                    return 2;
                case IssueCategory.Other:
                    return 4;
                default:
                    return 3;
            }
        }

        /// <summary>
        /// Free text locations from the Report Issues form cannot be turned
        /// into real coordinates without a mapping service, so a repeatable
        /// position on the grid is derived from the text instead. The same
        /// location text always lands on the same spot.
        /// </summary>
        private static void PseudoCoordinates(string location, out double x, out double y)
        {
            string text = (location ?? string.Empty).Trim().ToLowerInvariant();
            x = (StableHash(text) % 2000) / 100.0;
            y = (StableHash(text + "#y") % 2000) / 100.0;
        }

        private static int StableHash(string text)
        {
            unchecked
            {
                int hash = 17;
                foreach (char c in text)
                    hash = hash * 31 + c;
                return hash & 0x7FFFFFFF;
            }
        }

        // ---------- sample data ----------

        private static void Seed()
        {
            DateTime now = DateTime.Now;

            SeedRequest("SR-1001", "Roads", "Main Road, Ward 12", "Large pothole near the taxi rank is damaging vehicles.", 2, now.AddDays(-10), 2.0, 3.0, RequestStatus.Assigned);
            SeedRequest("SR-1002", "Water and Sewage", "5th Avenue, Ward 12", "Burst water pipe is flooding the street.", 1, now.AddDays(-6), 2.8, 3.5, RequestStatus.InProgress);
            SeedRequest("SR-1003", "Electricity", "Oak Street, Ward 7", "Streetlight has been out for two weeks.", 3, now.AddDays(-4), 8.0, 7.5, RequestStatus.Submitted);
            SeedRequest("SR-1004", "Sanitation", "Riverside Park", "Refuse has not been collected for a week.", 3, now.AddDays(-3), 12.5, 4.0, RequestStatus.Submitted);
            SeedRequest("SR-1005", "Roads", "9th Avenue, Ward 12", "Damaged road sign at the intersection.", 4, now.AddDays(-20), 3.5, 2.5, RequestStatus.Resolved);
            SeedRequest("SR-1006", "Utilities", "Civic Square", "Public water tap is broken and leaking.", 3, now.AddDays(-5), 7.0, 8.5, RequestStatus.Assigned);
            SeedRequest("SR-1007", "Water and Sewage", "Suburb B", "Very low water pressure for several days.", 2, now.AddDays(-2), 11.0, 5.0, RequestStatus.Submitted);
            SeedRequest("SR-1008", "Electricity", "Suburb C", "Exposed live wires hanging near a school.", 1, now.AddDays(-1), 8.8, 6.9, RequestStatus.Submitted);
            SeedRequest("SR-1009", "Sanitation", "Community Hall B", "Overflowing public bins attracting pests.", 3, now.AddDays(-7), 13.0, 3.2, RequestStatus.InProgress);
            SeedRequest("SR-1010", "Other", "Public Library", "Broken front door handle.", 4, now.AddDays(-15), 7.4, 9.0, RequestStatus.Resolved);
            SeedRequest("SR-1011", "Roads", "Heritage Road", "Flooded intersection after heavy rain.", 2, now.AddDays(-1), 16.0, 10.0, RequestStatus.Submitted);
            SeedRequest("SR-1012", "Utilities", "Municipal Sports Grounds", "Broken floodlight over the soccer field.", 3, now.AddDays(-8), 16.8, 9.2, RequestStatus.Assigned);

            RebuildGraph();
        }

        private static void SeedRequest(string id, string category, string location, string description,
            int priority, DateTime submitted, double x, double y, RequestStatus targetStatus)
        {
            var request = new ServiceRequest(id, category, location, description, priority, submitted, x, y);

            // Walk the request forward one stage at a time, a day apart, so its history looks realistic.
            while (request.Status < targetStatus)
                request.AdvanceStatus(submitted.AddDays((int)request.Status));

            AddCore(request);
        }
    }
}
