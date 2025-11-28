namespace BazeSec.DTOs.AnonymousTips
{
    public class UpdateTipStatusDto
    {
        public string Status { get; set; }        // New, InReview, Resolved, Dismissed
        public string Priority { get; set; }      // Low, Medium, High
        public string InternalNotes { get; set; } // Admin/Security notes
    }
}
