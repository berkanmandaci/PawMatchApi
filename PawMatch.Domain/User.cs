using System.Collections.Generic;
using System.Linq;

namespace PawMatch.Domain
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string? Bio { get; set; }
        public bool HasPet { get; set; } = false;
        public bool HasProfile { get; set; } = false;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<Photo> Photos { get; set; } = new();
        public List<Pet> Pets { get; set; } = new();
    }

    public static class UserExtensions
    {
        public static List<int> GetPhotoIds(this User user)
            => user.Photos?.Select(p => p.Id).ToList() ?? new List<int>();

        public static List<int> GetPetIds(this User user)
            => user.Pets?.Select(p => p.Id).ToList() ?? new List<int>();
    }
} 