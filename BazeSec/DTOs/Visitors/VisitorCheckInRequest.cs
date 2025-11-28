namespace BazeSec.DTOs.Visitors
{
    public class VisitorCheckInRequest
    {
        public string FullName { get; set; }

        public string Email { get; set; }

        public string Phone { get; set; }

        // Purpose - must match one of the supported options
        public string Purpose { get; set; }

        // Detailed explanation - required for all purposes
        public string Reason { get; set; }

        // For Visit Staff/Student, Deliver Parcel, etc.
        public string RecipientName { get; set; }

        // For Visit Staff/Student and Deliver Parcel (11 digits)
        public string RecipientPhone { get; set; }

        // For department-based visits
        public string Department { get; set; }
    }
}
