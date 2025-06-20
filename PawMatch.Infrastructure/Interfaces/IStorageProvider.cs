using System.IO;
using System.Threading.Tasks;

namespace PawMatch.Infrastructure.Interfaces
{
    public interface IStorageProvider
    {
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream> DownloadAsync(string fileId);
        Task DeleteAsync(string fileId);
    }
} 