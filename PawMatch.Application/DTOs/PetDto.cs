namespace PawMatch.Application.DTOs
{
    public class PetDto
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Owner's ID
        public string Name { get; set; }
        public string Breed { get; set; }
        public int Age { get; set; }
        public string Description { get; set; }
        public List<string> PhotoIds { get; set; } // Google Drive File IDs
    }
} 