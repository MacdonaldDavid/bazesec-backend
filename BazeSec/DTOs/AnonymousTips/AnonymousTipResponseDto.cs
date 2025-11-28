using System;

namespace BazeSec.DTOs.AnonymousTips
{
    public class AnonymousTipResponseDto
    {
        public int Id { get; set; }
        public string ReporterRole { get; set; }

        public string Title { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }

        public string Status { get; set; }
        public string Priority { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
