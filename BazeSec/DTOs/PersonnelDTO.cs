namespace BazeSec.DTOs
{
    public class PersonnelCreateDTO
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Status { get; set; } = "Active";
        public string Guardpost { get; set; } = null!;
    }

    public class PersonnelUpdateDTO : PersonnelCreateDTO { }
}
