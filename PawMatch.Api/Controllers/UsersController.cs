using Microsoft.AspNetCore.Mvc;
using PawMatch.Application.DTOs;
using PawMatch.Application.Interfaces;
using System;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using PawMatch.Domain;

namespace PawMatch.Api.Controllers
{
    [ApiController]
    [Route("api/v1/users")]
    public class UsersController : BaseController
    {
        private readonly IUserService _userService;
        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        /// <summary>
        /// Kullanıcı girişi yapar.
        /// </summary>
        /// <remarks>
        /// Başarılı yanıt örneği:
        /// 
        ///     POST /api/v1/users/login
        ///     {
        ///         "email": "kullanici@example.com",
        ///         "password": "şifre"
        ///     }
        /// 
        /// Yanıt:
        ///     {
        ///         "status": "success",
        ///         "data": {
        ///             "user": {
        ///                 "id": 1,
        ///                 "name": "Ali Veli",
        ///                 "email": "kullanici@example.com",
        ///                 "hasProfile": true
        ///             },
        ///             "token": "jwt-token-string"
        ///         },
        ///         "error": null
        ///     }
        /// </remarks>
        /// <param name="dto">Kullanıcı giriş bilgileri</param>
        /// <returns>Kullanıcı ve JWT token</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            try
            {
                var result = await _userService.LoginAsync(dto);
                return Ok(new { data = result, status = "success", error = (string)null });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { data = (object)null, status = "error", error = ex.Message });
            }
        }

        /// <summary>
        /// Kullanıcı profilini günceller.
        /// </summary>
        /// <remarks>
        ///     PATCH /api/v1/users/profile
        ///     {
        ///         "name": "Ali Veli",
        ///         "bio": "Kısa biyografi",
        ///         "hasPet": true
        ///     }
        /// 
        /// Yanıt:
        ///     {
        ///         "status": "success",
        ///         "data": {
        ///             "user": {
        ///                 "id": 1,
        ///                 "name": "Ali Veli",
        ///                 "email": "kullanici@example.com",
        ///                 "hasProfile": true
        ///             },
        ///             "token": "jwt-token-string"
        ///         },
        ///         "error": null
        ///     }
        /// </remarks>
        /// <param name="dto">Profil güncelleme bilgileri</param>
        /// <returns>Güncellenmiş kullanıcı ve JWT token</returns>
        [HttpPatch("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });
            var result = await _userService.UpdateProfileAsync(userId.Value, dto);
            return Ok(new { data = result, status = "success", error = (string)null });
        }

        /// <summary>
        /// Yeni kullanıcı kaydı oluşturur.
        /// </summary>
        /// <remarks>
        ///     POST /api/v1/users/register
        ///     {
        ///         "name": "Ali Veli",
        ///         "email": "kullanici@example.com",
        ///         "password": "sifre123"
        ///     }
        /// 
        /// Yanıt:
        ///     {
        ///         "status": "success",
        ///         "data": {
        ///             "user": {
        ///                 "id": 1,
        ///                 "name": "Ali Veli",
        ///                 "email": "kullanici@example.com",
        ///                 "hasProfile": false
        ///             },
        ///             "token": "jwt-token-string"
        ///         },
        ///         "error": null
        ///     }
        /// </remarks>
        /// <param name="dto">Kullanıcı kayıt bilgileri</param>
        /// <returns>Kayıtlı kullanıcı ve JWT token</returns>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            var result = await _userService.RegisterAsync(dto);
            return Ok(new { data = result, status = "success", error = (string)null });
        }

        /// <summary>
        /// Kullanıcının kendi hesabını siler.
        /// </summary>
        /// <remarks>
        /// Sadece kimliği doğrulanmış kullanıcılar kendi hesaplarını silebilir.
        /// Yanıt:
        ///     {
        ///         "status": "success",
        ///         "data": null,
        ///         "error": null
        ///     }
        /// </remarks>
        /// <returns>Başarı veya hata mesajı</returns>
        [HttpDelete("me")]
        [Authorize]
        public async Task<IActionResult> DeleteMyAccount()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });

            try
            {
                await _userService.DeleteUserAsync(userId.Value);
                return Ok(new { status = "success", data = (object)null, error = (string)null });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { status = "error", error = ex.Message });
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using a logger)
                return StatusCode(500, new { status = "error", error = "Hesap silinirken bir hata oluştu." });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized(new ApiResponse<object> { Status = "Error", Error = "Invalid user ID in token." });
            }

            var user = await _userService.GetUserByIdAsync(userId);
            var userDto = UserPrivateDtoMapper.ToPrivateDto(user, user.GetPhotoIds(), user.GetPetIds());
            return Ok(new ApiResponse<UserPrivateDto> { Data = userDto, Status = "Success" });
        }
    }
}
