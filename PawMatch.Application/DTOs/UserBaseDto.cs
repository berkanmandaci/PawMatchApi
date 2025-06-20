using System.Collections.Generic;

namespace PawMatch.Application.DTOs
{
    public class UserBaseDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Bio { get; set; }
        public bool HasPet { get; set; }
        public bool HasProfile { get; set; }
        public List<int> PhotoIds { get; set; }
        public int? Age { get; set; }
        public string? Gender { get; set; }
        public List<int> PetIds { get; set; }
    }
} 