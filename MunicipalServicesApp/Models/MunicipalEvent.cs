using System;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// A local municipal event or announcement that citizens can browse,
    /// search for, and receive recommendations about.
    /// </summary>
    public class MunicipalEvent
    {
        public Guid Id { get; }
        public string Title { get; set; }
        public string Category { get; set; }
        public DateTime EventDate { get; set; }
        public string Location { get; set; }
        public string Description { get; set; }

        /// <summary>
        /// Featured events (e.g. urgent safety notices) are pushed through
        /// the priority queue ahead of ordinary events with the same date.
        /// </summary>
        public bool IsFeatured { get; set; }

        public MunicipalEvent(string title, string category, DateTime eventDate, string location, string description, bool isFeatured = false)
        {
            Id = Guid.NewGuid();
            Title = title;
            Category = category;
            EventDate = eventDate;
            Location = location;
            Description = description;
            IsFeatured = isFeatured;
        }

        public override string ToString()
        {
            return $"{Title} ({Category}) - {EventDate:ddd, dd MMM yyyy}";
        }
    }
}
