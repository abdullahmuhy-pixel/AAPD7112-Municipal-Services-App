using System;
using System.Collections.Generic;
using System.Linq;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// Central in-memory store for Local Events and Announcements.
    ///
    /// This class deliberately keeps several different data structures in
    /// sync with one another, each one chosen because it is the natural
    /// fit for a specific access pattern required by the brief:
    ///
    ///   - SortedDictionary&lt;DateTime, List&lt;MunicipalEvent&gt;&gt;: events kept in
    ///     chronological order automatically, for the main "upcoming
    ///     events" display and for date-range searching.
    ///   - Dictionary&lt;string, List&lt;MunicipalEvent&gt;&gt;: events grouped by
    ///     category (a hash table), for fast category-based search.
    ///   - HashSet&lt;string&gt; / HashSet&lt;DateTime&gt;: the unique set of
    ///     categories and dates currently in use, so the search filters
    ///     never show duplicate options.
    ///   - Queue&lt;MunicipalEvent&gt;: a FIFO "latest announcements" feed in the
    ///     order events were added to the system.
    ///   - Stack&lt;MunicipalEvent&gt;: a LIFO "recently viewed" history, so the
    ///     most recently opened event is always shown first.
    ///   - CustomPriorityQueue&lt;MunicipalEvent&gt;: featured / urgent events
    ///     ordered by how soon they occur.
    /// </summary>
    public static class EventRepository
    {
        private static readonly SortedDictionary<DateTime, List<MunicipalEvent>> _eventsByDate
            = new SortedDictionary<DateTime, List<MunicipalEvent>>();

        private static readonly Dictionary<string, List<MunicipalEvent>> _eventsByCategory
            = new Dictionary<string, List<MunicipalEvent>>();

        private static readonly HashSet<string> _categories = new HashSet<string>();
        private static readonly HashSet<DateTime> _dates = new HashSet<DateTime>();

        private static readonly Queue<MunicipalEvent> _announcementFeed = new Queue<MunicipalEvent>();
        private static readonly Stack<MunicipalEvent> _recentlyViewed = new Stack<MunicipalEvent>();
        private static readonly CustomPriorityQueue<MunicipalEvent> _featuredQueue = new CustomPriorityQueue<MunicipalEvent>();

        public static IReadOnlyCollection<string> Categories => _categories;
        public static IReadOnlyCollection<DateTime> Dates => _dates;
        public static Queue<MunicipalEvent> AnnouncementFeed => _announcementFeed;
        public static Stack<MunicipalEvent> RecentlyViewed => _recentlyViewed;

        static EventRepository()
        {
            SeedSampleEvents();
        }

        public static void Add(MunicipalEvent ev)
        {
            if (ev == null) return;

            var dateOnly = ev.EventDate.Date;

            if (!_eventsByDate.ContainsKey(dateOnly))
                _eventsByDate[dateOnly] = new List<MunicipalEvent>();
            _eventsByDate[dateOnly].Add(ev);

            if (!_eventsByCategory.ContainsKey(ev.Category))
                _eventsByCategory[ev.Category] = new List<MunicipalEvent>();
            _eventsByCategory[ev.Category].Add(ev);

            _categories.Add(ev.Category);
            _dates.Add(dateOnly);

            _announcementFeed.Enqueue(ev);

            double daysUntil = (ev.EventDate.Date - DateTime.Now.Date).TotalDays;
            // Featured events jump ahead of non-featured events on the same day.
            double priority = ev.IsFeatured ? daysUntil - 0.5 : daysUntil;
            _featuredQueue.Enqueue(ev, priority);
        }

        /// <summary>All events, in chronological order (via the SortedDictionary).</summary>
        public static List<MunicipalEvent> AllEventsChronological()
        {
            return _eventsByDate.Values.SelectMany(list => list).ToList();
        }

        /// <summary>The soonest-occurring events, most urgent first (via the priority queue).</summary>
        public static List<MunicipalEvent> FeaturedByUrgency(int take = 5)
        {
            return _featuredQueue.ToOrderedList().Take(take).ToList();
        }

        /// <summary>Search by category and/or date. Either filter may be null/empty to be ignored.</summary>
        public static List<MunicipalEvent> Search(string category, DateTime? date)
        {
            IEnumerable<MunicipalEvent> results = AllEventsChronological();

            if (!string.IsNullOrWhiteSpace(category))
            {
                results = _eventsByCategory.ContainsKey(category)
                    ? _eventsByCategory[category]
                    : Enumerable.Empty<MunicipalEvent>();
            }

            if (date.HasValue)
            {
                var dateOnly = date.Value.Date;
                results = results.Where(e => e.EventDate.Date == dateOnly);
            }

            return results.OrderBy(e => e.EventDate).ToList();
        }

        public static void MarkViewed(MunicipalEvent ev)
        {
            if (ev != null) _recentlyViewed.Push(ev);
        }

        private static void SeedSampleEvents()
        {
            var today = DateTime.Now.Date;

            Add(new MunicipalEvent("Ward Community Meeting", "Community Meeting", today.AddDays(3).AddHours(18),
                "Ward 12 Community Hall", "Monthly ward meeting to discuss service delivery feedback and upcoming projects."));

            Add(new MunicipalEvent("Free Flu Vaccination Drive", "Health and Wellness", today.AddDays(1).AddHours(9),
                "Clinic Site, Main Street", "Free seasonal flu vaccinations for residents, no appointment needed.", isFeatured: true));

            Add(new MunicipalEvent("Youth Soccer Tournament", "Sports and Recreation", today.AddDays(6).AddHours(10),
                "Municipal Sports Grounds", "Inter-ward youth soccer tournament, open to teams aged 14 to 18."));

            Add(new MunicipalEvent("Heritage Day Market", "Arts and Culture", today.AddDays(10).AddHours(8),
                "Civic Square", "Local crafts, food stalls and live music celebrating community heritage."));

            Add(new MunicipalEvent("Water Supply Interruption Notice", "Safety and Awareness", today.AddDays(2).AddHours(6),
                "Suburb B and C", "Planned maintenance will interrupt water supply from 06:00 to 18:00.", isFeatured: true));

            Add(new MunicipalEvent("Road Resurfacing Notice", "Infrastructure Update", today.AddDays(5).AddHours(7),
                "Main Road between 5th and 9th Ave", "Expect delays while resurfacing work takes place during the day."));

            Add(new MunicipalEvent("Tree Planting Day", "Environment", today.AddDays(8).AddHours(9),
                "Riverside Park", "Volunteers welcome to help plant indigenous trees along the riverbank."));

            Add(new MunicipalEvent("Youth Coding Workshop", "Youth Programs", today.AddDays(4).AddHours(14),
                "Public Library Hall", "Free introductory coding workshop for high school learners."));

            Add(new MunicipalEvent("Load Shedding Schedule Update", "Safety and Awareness", today.AddDays(1).AddHours(5),
                "All Wards", "Updated load shedding stages and schedule for the coming week.", isFeatured: true));

            Add(new MunicipalEvent("Senior Citizens Health Check", "Health and Wellness", today.AddDays(7).AddHours(9),
                "Community Hall B", "Free blood pressure and glucose screening for residents over 60."));
        }
    }
}
