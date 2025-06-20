namespace PawMatch.Application.DTOs
{
    public class ApiResponse<T>
    {
        public T Data { get; set; }
        public string Status { get; set; }
        public object? Error { get; set; } // Can be null or an error object
    }

    public class UserAuthResponseDto
    {
        public UserPrivateDto UserPrivate { get; set; }
        public string Token { get; set; }
    }

    public class UserPrivateDto : UserBaseDto
    {
        public string Email { get; set; }
        public string PasswordHash { get; set; } // Sadece burada!
        public List<int> PetIds { get; set; }
    }

    public static class UserPrivateDtoMapper
    {
        public static UserPrivateDto ToPrivateDto(PawMatch.Domain.User user, List<int> photoIds, List<int> petIds)
        {
            return new UserPrivateDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Bio = user.Bio,
                HasPet = user.HasPet,
                HasProfile = user.HasProfile,
                PhotoIds = photoIds,
                PetIds = petIds,
                // Age, Gender eklenirse buraya eklenir
                PasswordHash = user.PasswordHash
            };
        }
    }
} 