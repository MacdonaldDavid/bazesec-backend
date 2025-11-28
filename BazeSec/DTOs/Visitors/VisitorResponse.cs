using System;

namespace BazeSec.DTOs.Visitors
{
    public class VisitorResponse
    {
        public int Id { get; set; }
        public string VisitorCode { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        public string Purpose { get; set; }
        public string Reason { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string Department { get; set; }

        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public DateTime? ApprovalTime { get; set; }
        public DateTime? RejectionTime { get; set; }
        public string RejectionComment { get; set; }
    }
}
