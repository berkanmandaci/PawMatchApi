using System.ComponentModel.DataAnnotations;

namespace PawMatch.Application.DTOs
{
    public class UserRegisterDto
    {
        [Required]
        public string Name { get; set; }
        [Required, EmailAddress]
        public string Email { get; set; }
        [Required, MinLength(6)]
        public string Password { get; set; }
        // Örnek: public int? Age { get; set; }
        // Örnek: public string? Gender { get; set; }
    }
} 