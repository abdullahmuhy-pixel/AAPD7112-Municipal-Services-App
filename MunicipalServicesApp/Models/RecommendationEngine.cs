using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// Implements the "Additional Recommendation Feature": analyses the
    /// citizen's search patterns (which categories they search for, and how
    /// often) and uses that to suggest related events they have not yet
    /// viewed.
    ///
    /// The algorithm is a simple frequency-based content scorer:
    ///   1. Every search records a point against its category in
    ///      _categorySearchCounts (a Dictionary&lt;string, int&gt;).
    ///   2. Recommendations score each candidate event by its category's
    ///      search count, so events from categories the citizen searches
    ///      for often are suggested first.
    ///   3. Events already opened (tracked in _viewedEventIds, a HashSet)
    ///      are excluded, so recommendations always point to something new.
    /// </summary>
    public class RecommendationEngine
    {
        private readonly Dictionary<string, int> _categorySearchCounts = new Dictionary<string, int>();
        private readonly Queue<string> _recentSearchLog = new Queue<string>();
        private readonly HashSet<Guid> _viewedEventIds = new HashSet<Guid>();

        private const int MaxSearchLogEntries = 20;

        public void RecordSearch(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return;

            if (_categorySearchCounts.ContainsKey(category))
                _categorySearchCounts[category]++;
            else
                _categorySearchCounts[category] = 1;

            _recentSearchLog.Enqueue(category);
            if (_recentSearchLog.Count > MaxSearchLogEntries)
                _recentSearchLog.Dequeue();
        }

        public void RecordViewed(MunicipalEvent ev)
        {
            if (ev != null) _viewedEventIds.Add(ev.Id);
        }

        /// <summary>
        /// Returns up to `take` recommended events, scored by how often the
        /// user has searched for their category, excluding events already
        /// viewed. Ties are broken by soonest date first.
        /// </summary>
        public List<MunicipalEvent> GetRecommendations(int take = 5)
        {
            if (_categorySearchCounts.Count == 0)
                return new List<MunicipalEvent>();

            var candidates = EventRepository.AllEventsChronological()
                .Where(e => !_viewedEventIds.Contains(e.Id))
                .Select(e => new
                {
                    Event = e,
                    Score = _categorySearchCounts.ContainsKey(e.Category) ? _categorySearchCounts[e.Category] : 0
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Event.EventDate)
                .Take(take)
                .Select(x => x.Event)
                .ToList();

            return candidates;
        }

        /// <summary>The category the user has searched for most often, if any.</summary>
        public string TopSearchedCategory()
        {
            if (_categorySearchCounts.Count == 0) return null;
            return _categorySearchCounts.OrderByDescending(kv => kv.Value).First().Key;
        }
    }
}
