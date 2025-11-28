namespace BazeSec.Models
{
    public class Personnel
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string Email { get; set; } = null!;

        public string Status { get; set; } = "Active"; // Active / Inactive

        public string Guardpost { get; set; } = null!;
    }
}
