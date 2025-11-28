using System;

namespace BazeSec.Models
{
    public class KeyLog
    {
        public int Id { get; set; }

        public int KeyItemId { get; set; }
        public KeyItem KeyItem { get; set; } = null!;

        public int BorrowerId { get; set; }
        public string BorrowerName { get; set; } = null!;
        public string BorrowerRole { get; set; } = null!;

        public string Location { get; set; } = null!;

        public string Action { get; set; } = null!; // CheckOut / CheckIn

        public DateTime Timestamp { get; set; }
    }
}
