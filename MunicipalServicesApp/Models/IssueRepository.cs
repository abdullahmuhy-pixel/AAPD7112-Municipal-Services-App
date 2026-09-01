using System.Collections.Generic;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// In-memory store for reported issues. A single static List&lt;Issue&gt;
    /// is used across the application (Technical Requirement: "Utilise
    /// appropriate data structures to store user-reported issues").
    ///
    /// This is deliberately a simple in-process store for Part 1 — Part 2
    /// and Part 3 of the PoE extend the application with additional data
    /// structures (stacks/queues/dictionaries/sets for events, trees/graphs
    /// for service request status) that can read from this same list.
    /// </summary>
    public static class IssueRepository
    {
        private static readonly List<Issue> _issues = new List<Issue>();

        public static IReadOnlyList<Issue> Issues => _issues.AsReadOnly();

        public static void Add(Issue issue)
        {
            if (issue == null) return;
            _issues.Add(issue);
        }

        public static int Count => _issues.Count;
    }
}
