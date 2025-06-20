using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using PawMatch.Infrastructure.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PawMatch.Infrastructure.Providers
{
    public class GoogleDriveStorageProvider : IStorageProvider
    {
        private readonly DriveService _driveService;

        public GoogleDriveStorageProvider()
        {
            var credentialsPath = Environment.GetEnvironmentVariable("GoogleDrive__CredentialsPath")
                ?? "api/credentials/credentials.json";
            var credential = GoogleCredential.FromFile(credentialsPath)
                .CreateScoped(DriveService.Scope.Drive);
            _driveService = new DriveService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "PawMatch"
            });
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            var fileMetadata = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Parents = new[] { "root" }
            };
            var request = _driveService.Files.Create(fileMetadata, fileStream, contentType);
            request.Fields = "id";
            var file = await request.UploadAsync();
            if (file.Status != Google.Apis.Upload.UploadStatus.Completed)
                throw new Exception("Google Drive upload failed: " + file.Exception?.Message);
            return request.ResponseBody.Id;
        }

        public async Task<Stream> DownloadAsync(string fileId)
        {
            var stream = new MemoryStream();
            var request = _driveService.Files.Get(fileId);
            await request.DownloadAsync(stream);
            stream.Position = 0;
            return stream;
        }

        public async Task DeleteAsync(string fileId)
        {
            await _driveService.Files.Delete(fileId).ExecuteAsync();
        }
    }
} 