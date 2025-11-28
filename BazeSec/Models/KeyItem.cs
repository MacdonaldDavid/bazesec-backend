using System;

namespace BazeSec.Models
{
    public class KeyItem
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!; // FrontGate, Block A, Block B, etc.
        public string Status { get; set; } = "Available";

        public int? BorrowerId { get; set; }
        public string? BorrowerName { get; set; }
        public string? BorrowerRole { get; set; }

        public DateTime? CheckedOutAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
    }
}
