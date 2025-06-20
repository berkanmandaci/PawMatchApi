using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawMatch.Application.DTOs;
using PawMatch.Application.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace PawMatch.Api.Controllers
{
    [ApiController]
    [Route("api/v1/photos")]
    [Authorize]
    public class PhotosController : BaseController
    {
        private readonly IPhotoService _photoService;

        public PhotosController(IPhotoService photoService)
        {
            _photoService = photoService;
        }

        public class UserPhotoUploadRequest
        {
            public IFormFile File { get; set; }
        }

        public class PetPhotoUploadRequest
        {
            public IFormFile File { get; set; }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });

                var photoStream = await _photoService.GetPhotoStreamAsync(id, userId.Value);
                // Content-Type ve Content-Disposition ayarla
                // (Tipi DB'den de çekebilirsin, burada örnek olarak image/jpeg)
                return File(photoStream, "image/jpeg", enableRangeProcessing: false);
            }
            catch (Exception ex)
            {
                return NotFound(new { status = "error", error = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int id)
        {
            try
            {
                await _photoService.DeletePhotoAsync(id);
                return Ok(new { status = "success" });
            }
            catch (Exception ex)
            {
                return NotFound(new { status = "error", error = ex.Message });
            }
        }

        // Kullanıcıya fotoğraf yükleme
        [HttpPost("user")]
        public async Task<IActionResult> UploadUserPhoto([FromForm] UserPhotoUploadRequest request)
        {
            var file = request.File;
            if (file == null || (file.ContentType != "image/jpeg" && file.ContentType != "image/png"))
                return BadRequest(new { status = "error", error = "Sadece JPEG veya PNG dosya yüklenebilir." });
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { status = "error", error = "Dosya boyutu 5 MB'dan büyük olamaz." });
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });
            using var stream = file.OpenReadStream();
            var result = await _photoService.UploadPhotoAsync(
                new PhotoUploadDto { UserId = userId.Value },
                stream,
                file.FileName,
                file.ContentType
            );
            return Ok(new ApiResponse<PhotoDto> { Data = result, Status = "success" });
        }

        // Pet'e fotoğraf yükleme
        [HttpPost("users/pets/{petId}/photos")]
        [Authorize]
        public async Task<IActionResult> UploadPetPhoto(int petId, [FromForm] PetPhotoUploadRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });
            // Dummy sahiplik kontrolü (gerçek uygulamada DB'den kontrol edilmeli)
            // if (!await _petService.UserOwnsPet(userId.Value, petId)) return Forbid();
            var file = request.File;
            if (file == null || (file.ContentType != "image/jpeg" && file.ContentType != "image/png"))
                return BadRequest(new { status = "error", error = "Sadece JPEG veya PNG dosya yüklenebilir." });
            if (file.Length > 5 * 1024 * 1024)
                return BadRequest(new { status = "error", error = "Dosya boyutu 5 MB'dan büyük olamaz." });
            using var stream = file.OpenReadStream();
            var result = await _photoService.UploadPhotoAsync(
                new PhotoUploadDto { PetId = petId },
                stream,
                file.FileName,
                file.ContentType
            );
            return Ok(new ApiResponse<PhotoDto> { Data = result, Status = "success" });
        }
    }
} 