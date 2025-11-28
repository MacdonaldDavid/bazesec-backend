namespace BazeSec.DTOs
{
    public class KeyCreateDTO
    {
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
    }

    public class KeyUpdateDTO
    {
        public string Name { get; set; } = null!;
        public string Location { get; set; } = null!;
        public string Status { get; set; } = "Available";
    }

    public class KeyActionDTO
    {
        public string ScannedLocation { get; set; } = null!;
    }
}
