using System.Threading.Tasks;
using PawMatch.Application.DTOs;
using PawMatch.Application.Interfaces;
using PawMatch.Domain;
using PawMatch.Infrastructure.Interfaces;
using PawMatch.Infrastructure;
using BCrypt.Net;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace PawMatch.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IJwtProvider _jwtProvider;
        private readonly IUserRepository _userRepository;
        private readonly IPhotoService _photoService;
        private readonly AppDbContext _dbContext;

        public UserService(IUserRepository userRepository, IJwtProvider jwtProvider, IPhotoService photoService, AppDbContext dbContext)
        {
            _userRepository = userRepository;
            _jwtProvider = jwtProvider;
            _photoService = photoService;
            _dbContext = dbContext;
        }
        public async Task<UserAuthResponseDto> RegisterAsync(UserRegisterDto dto)
        {
            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };
            await _userRepository.AddAsync(user);
            var token = _jwtProvider.GenerateToken(user);
            
            var userPhotos = await _dbContext.Photos
                                            .Where(p => p.UserId == user.Id)
                                            .Select(p => p.Id)
                                            .ToListAsync();

            return new UserAuthResponseDto
            {
                UserPrivate = new UserPrivateDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Bio = user.Bio,
                    HasPet = user.HasPet,
                    HasProfile = !string.IsNullOrWhiteSpace(user.Name) && !string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Bio) && user.HasPet,
                    PhotoIds = userPhotos
                },
                Token = token
            };
        }
        public async Task<UserAuthResponseDto> LoginAsync(UserLoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new UnauthorizedAccessException("Geçersiz kimlik bilgileri");
            var token = _jwtProvider.GenerateToken(user);

            var userPhotos = await _dbContext.Photos
                                            .Where(p => p.UserId == user.Id)
                                            .Select(p => p.Id)
                                            .ToListAsync();

            return new UserAuthResponseDto
            {
                UserPrivate = new UserPrivateDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Bio = user.Bio,
                    HasPet = user.HasPet,
                    HasProfile = !string.IsNullOrWhiteSpace(user.Name) && !string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Bio) && user.HasPet,
                    PhotoIds = userPhotos
                },
                Token = token
            };
        }
        public async Task<UserAuthResponseDto> UpdateProfileAsync(int id, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new KeyNotFoundException("Kullanıcı bulunamadı");
            user.Name = dto.Name;
            user.Bio = dto.Bio;
            user.HasPet = dto.HasPet;
            await _userRepository.UpdateAsync(user);
            var token = _jwtProvider.GenerateToken(user);

            var userPhotos = await _dbContext.Photos
                                            .Where(p => p.UserId == user.Id)
                                            .Select(p => p.Id)
                                            .ToListAsync();

            return new UserAuthResponseDto
            {
                UserPrivate = new UserPrivateDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email,
                    Bio = user.Bio,
                    HasPet = user.HasPet,
                    HasProfile = !string.IsNullOrWhiteSpace(user.Name) && !string.IsNullOrWhiteSpace(user.Email) && !string.IsNullOrWhiteSpace(user.Bio) && user.HasPet,
                    PhotoIds = userPhotos
                },
                Token = token
            };
        }
        public async Task DeleteUserAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var pets = _dbContext.Pets.Where(p => p.UserId == id).ToList();
            foreach (var pet in pets)
            {
                await _photoService.DeletePetPhotosAsync(pet.Id);
                _dbContext.Pets.Remove(pet);
            }
            await _dbContext.SaveChangesAsync();

            await _photoService.DeleteUserPhotosAsync(id);
            await _userRepository.DeleteAsync(id);
        }

        /// <summary>
        /// API response için: Kullanıcıyı DTO olarak döndürür (login, register, profil için).
        /// </summary>
        public async Task<User> GetUserByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                throw new KeyNotFoundException("User not found.");
            }

            var userPhotos = await _dbContext.Photos
                                            .Where(p => p.UserId == user.Id)
                                            .Select(p => p)
                                            .ToListAsync();
            var petIds = await _dbContext.Pets
                                        .Where(p => p.UserId == user.Id)
                                        .Select(p => p)
                                        .ToListAsync();

            return new User
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Bio = user.Bio,
                HasPet = user.HasPet,
                HasProfile = user.HasProfile,
                Photos = userPhotos,
                Pets = petIds
            };
        }

        /// <summary>
        /// Domain işlemleri için: Kullanıcıyı entity olarak döndürür.
        /// </summary>
        public async Task<User> GetUserDomainByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        /// <summary>
        /// Domain işlemleri için: Birden fazla kullanıcıyı entity olarak döndürür.
        /// </summary>
        public async Task<List<User>> GetUsersByIdsAsync(List<int> ids)
        {
            return await _userRepository.GetByIdsAsync(ids);
        }
    }
} 