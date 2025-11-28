using System;

namespace BazeSec.Models
{
    public class EmergencyAlert
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;

        // Critical / Warning / Info
        public string Severity { get; set; } = "Info";

        public string? Location { get; set; }

        // Active / Resolved
        public string Status { get; set; } = "Active";

        public string? ResolutionNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedByUserId { get; set; }
        public string? ResolvedByName { get; set; }
    }
}
