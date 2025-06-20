using System.Collections.Generic;
using PawMatch.Domain;

namespace PawMatch.Application.DTOs
{
    public class UserPublicDto : UserBaseDto
    {
        // Sadece public alanlar, ekstra yok
    }

    public static class UserPublicDtoMapper
    {
        public static UserPublicDto ToPublicDto(User user, List<int> photoIds, List<int> petIds)
        {
            return new UserPublicDto
            {
                Id = user.Id,
                Name = user.Name,
                Bio = user.Bio,
                HasPet = user.HasPet,
                HasProfile = user.HasProfile,
                PhotoIds = photoIds,
                PetIds = petIds,
                Age = null, // user.Age,
                Gender = null // user.Gender
            };
        }
    }
} 