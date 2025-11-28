using System;

namespace BazeSec.Models
{
    public class AnonymousTip
    {
        public int Id { get; set; }

        // Reporter metadata (auto-filled from JWT)
        public int ReporterUserId { get; set; }
        public string ReporterRole { get; set; }
        public string PreferredContact { get; set; }

        // Main content
        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }

        // Status tracking
        public string Status { get; set; } = "New";       // New, InReview, Resolved, Dismissed
        public string Priority { get; set; } = "Medium";  // Low, Medium, High

        // Admin/Security handler
        public int? HandlerUserId { get; set; }
        public string HandlerName { get; set; }
        public string InternalNotes { get; set; }

        // System timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdatedAt { get; set; }
    }
}
