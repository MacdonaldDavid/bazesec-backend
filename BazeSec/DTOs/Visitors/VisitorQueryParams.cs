using System;

namespace BazeSec.DTOs.Visitors
{
    public class VisitorQueryParams
    {
        public string Status { get; set; } // PendingApproval, CheckedIn, CheckedOut, Rejected, or null for all
        public string Search { get; set; } // name/email/phone search
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
