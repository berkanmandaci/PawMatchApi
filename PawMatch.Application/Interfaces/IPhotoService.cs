using System.IO;
using System.Threading.Tasks;
using PawMatch.Application.DTOs;

namespace PawMatch.Application.Interfaces
{
    public interface IPhotoService
    {
        Task<PhotoDto> UploadPhotoAsync(PhotoUploadDto dto, Stream fileStream, string fileName = null, string contentType = null);
        Task<Stream> GetPhotoStreamAsync(int photoId, int userId);
        Task DeletePhotoAsync(int photoId);
        Task DeleteUserPhotosAsync(int userId);
        Task DeletePetPhotosAsync(int petId);
    }
} 