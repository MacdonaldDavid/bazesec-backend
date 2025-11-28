namespace BazeSec.DTOs.Emergencies
{
    public class CreateEmergencyAlertDto
    {
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Severity { get; set; } = "Info";  // Critical / Warning / Info
        public string? Location { get; set; }
    }

    public class ResolveEmergencyRequest
    {
        public string? ResolutionNote { get; set; }
    }
}
