using System.Threading.Tasks;
using PawMatch.Application.DTOs;
using System.Collections.Generic;
using PawMatch.Domain;

namespace PawMatch.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserAuthResponseDto> RegisterAsync(UserRegisterDto dto);
        Task<UserAuthResponseDto> LoginAsync(UserLoginDto dto);
        Task<UserAuthResponseDto> UpdateProfileAsync(int id, UpdateProfileDto dto);
        Task DeleteUserAsync(int id);
        Task<User> GetUserByIdAsync(int id);
        Task<List<User>> GetUsersByIdsAsync(List<int> ids);
        Task<User> GetUserDomainByIdAsync(int id);
    }
} 