using System;

namespace MunicipalServicesApp.Models
{
    /// <summary>
    /// Categories a citizen can select when reporting a municipal issue.
    /// </summary>
    public enum IssueCategory
    {
        Sanitation,
        Roads,
        WaterAndSewage,
        Electricity,
        Utilities,
        Other
    }

    /// <summary>
    /// Represents a single issue reported by a citizen through the
    /// Report Issues feature. Instances are stored in-memory in
    /// IssueRepository using a List&lt;Issue&gt;.
    /// </summary>
    public class Issue
    {
        public Guid Id { get; }
        public string Location { get; set; }
        public IssueCategory Category { get; set; }
        public string Description { get; set; }
        public string AttachmentPath { get; set; }
        public DateTime DateReported { get; }
        public string Status { get; set; }

        public Issue(string location, IssueCategory category, string description, string attachmentPath)
        {
            Id = Guid.NewGuid();
            Location = location;
            Category = category;
            Description = description;
            AttachmentPath = attachmentPath;
            DateReported = DateTime.Now;
            Status = "Submitted";
        }

        /// <summary>
        /// A short, human-readable reference number derived from the Issue's
        /// Guid, shown to the citizen as confirmation of their report.
        /// </summary>
        public string ReferenceNumber => Id.ToString().Substring(0, 8).ToUpperInvariant();

        public override string ToString()
        {
            return $"[{ReferenceNumber}] {Category} at {Location} — {Status}";
        }
    }
}
