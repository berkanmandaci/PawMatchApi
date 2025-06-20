using System.ComponentModel.DataAnnotations;

namespace PawMatch.Application.DTOs
{
    public class UpdateProfileDto
    {
        [Required]
        public string Name { get; set; }
        [StringLength(500)]
        public string Bio { get; set; }
        public bool HasPet { get; set; }
        // Örnek: public int? Age { get; set; }
        // Örnek: public string? Gender { get; set; }
    }
} 