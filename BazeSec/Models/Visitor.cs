using System;

namespace BazeSec.Models
{
    public class Visitor
    {
        public int Id { get; set; }

        public string? VisitorCode { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string? Phone { get; set; }

        public string Purpose { get; set; } = string.Empty;

        public string Reason { get; set; } = string.Empty;

        public string? RecipientName { get; set; }

        public string? RecipientPhone { get; set; }

        public string? Department { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? CheckInTime { get; set; }

        public DateTime? CheckOutTime { get; set; }

        public DateTime? ApprovalTime { get; set; }

        public DateTime? RejectionTime { get; set; }

        public string? RejectionComment { get; set; }
    }
}
