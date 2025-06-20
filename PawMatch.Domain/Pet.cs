using System.Collections.Generic;

namespace PawMatch.Domain
{
    public class Pet
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Age { get; set; }
        public string? Gender { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public List<Photo> Photos { get; set; } = new();
    }
} 