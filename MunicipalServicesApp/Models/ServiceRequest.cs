using System;
using System.Collections.Generic;

namespace MunicipalServicesApp.Models
{
    /// <summary>The stages a service request moves through, in order.</summary>
    public enum RequestStatus
    {
        Submitted = 1,
        Assigned = 2,
        InProgress = 3,
        Resolved = 4
    }

    /// <summary>
    /// A municipal service request that citizens can track by its unique ID.
    /// Requests come from two places: sample data seeded at start up, and
    /// issues reported through the Report Issues feature (Part 1), which
    /// reuse the issue's reference number as the request ID.
    /// </summary>
    public class ServiceRequest
    {
        public string Id { get; }
        public string Category { get; }
        public string Location { get; }
        public string Description { get; }
        public string AttachmentPath { get; }

        /// <summary>1 = Critical, 2 = High, 3 = Medium, 4 = Low.</summary>
        public int Priority { get; }

        public DateTime DateSubmitted { get; }
        public RequestStatus Status { get; private set; }

        /// <summary>
        /// Map position in km on a simple grid. Used to work out how far
        /// apart two requests are. (A real system would use GPS coordinates.)
        /// </summary>
        public double X { get; }
        public double Y { get; }

        public List<string> History { get; } = new List<string>();

        public ServiceRequest(string id, string category, string location, string description,
            int priority, DateTime dateSubmitted, double x, double y, string attachmentPath = null)
        {
            Id = id;
            Category = category;
            Location = location;
            Description = description;
            Priority = priority;
            DateSubmitted = dateSubmitted;
            X = x;
            Y = y;
            AttachmentPath = attachmentPath;
            Status = RequestStatus.Submitted;
            History.Add(FormatHistory(dateSubmitted, Status));
        }

        public string StatusText { get { return StatusToText(Status); } }

        public string PriorityText
        {
            get
            {
                switch (Priority)
                {
                    case 1: return "Critical";
                    case 2: return "High";
                    case 3: return "Medium";
                    default: return "Low";
                }
            }
        }

        public static string StatusToText(RequestStatus status)
        {
            return status == RequestStatus.InProgress ? "In Progress" : status.ToString();
        }

        /// <summary>Moves the request to its next stage. Returns false if it is already Resolved.</summary>
        public bool AdvanceStatus(DateTime? when = null)
        {
            if (Status == RequestStatus.Resolved) return false;

            DateTime stamp = when ?? DateTime.Now;
            if (stamp > DateTime.Now) stamp = DateTime.Now;

            Status = (RequestStatus)((int)Status + 1);
            History.Add(FormatHistory(stamp, Status));
            return true;
        }

        public double DistanceTo(ServiceRequest other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static string FormatHistory(DateTime when, RequestStatus status)
        {
            return when.ToString("dd MMM yyyy HH:mm") + "   " + StatusToText(status);
        }

        public override string ToString()
        {
            return Id + " (" + Category + ", " + StatusText + ")";
        }
    }
}
