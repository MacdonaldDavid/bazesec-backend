using System;
using System.Collections.Generic;

namespace BazeSec.Models
{
    public class LostFoundItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string ReporterContact { get; set; }
        public List<string> Images { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
