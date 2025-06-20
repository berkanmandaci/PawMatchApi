using System;
using System.IO;
using System.Threading.Tasks;
using PawMatch.Application.DTOs;
using PawMatch.Application.Interfaces;
using PawMatch.Domain;
using PawMatch.Infrastructure.Interfaces;
using PawMatch.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;

namespace PawMatch.Application.Services
{
    public class PhotoService : IPhotoService
    {
        private readonly AppDbContext _db;
        private readonly IStorageProvider _storageProvider;
        private readonly IDiscoverService _discoverService;

        public PhotoService(AppDbContext db, IStorageProvider storageProvider, IDiscoverService discoverService)
        {
            _db = db;
            _storageProvider = storageProvider;
            _discoverService = discoverService;
        }

        public async Task<PhotoDto> UploadPhotoAsync(PhotoUploadDto dto, Stream fileStream, string fileName = null, string contentType = null)
        {
            // Sınır ve tip kontrolleri üst katmanda yapılmalı
            var fileId = await _storageProvider.UploadAsync(fileStream, fileName, contentType);
            var photo = new Photo
            {
                FileName = fileName,
                GoogleDriveFileId = fileId,
                UploadDate = DateTime.UtcNow,
                UserId = dto.UserId,
                PetId = dto.PetId
            };
            _db.Photos.Add(photo);
            await _db.SaveChangesAsync();
            return new PhotoDto
            {
                Id = photo.Id,
                FileName = photo.FileName,
                ContentType = contentType,
                GoogleDriveFileId = fileId,
                UploadDate = photo.UploadDate,
                UserId = photo.UserId,
                PetId = photo.PetId
            };
        }

        public async Task<Stream> GetPhotoStreamAsync(int photoId, int userId)
        {
            var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == photoId);
            if (photo == null) throw new Exception("Photo not found");

            // Sahiplik kontrolü
            if (photo.UserId == userId) return await _storageProvider.DownloadAsync(photo.GoogleDriveFileId);

            if (photo.PetId.HasValue)
            {
                var petOwnerId = await _db.Pets
                                            .Where(p => p.Id == photo.PetId.Value)
                                            .Select(p => p.UserId)
                                            .FirstOrDefaultAsync();
                if (petOwnerId == userId) return await _storageProvider.DownloadAsync(photo.GoogleDriveFileId);
            }
            if (photo.UserId.HasValue)
            {
                var discoveredUsers = await _discoverService.DiscoverUsersAsync(userId);
                var discoveredUserIds = discoveredUsers.Select(u => u.User.Id).ToList();
                if (discoveredUserIds.Contains(photo.UserId.Value))
                {
                    return await _storageProvider.DownloadAsync(photo.GoogleDriveFileId);
                }
            }
            throw new UnauthorizedAccessException("You are not authorized to view this photo.");
        }

        public async Task DeletePhotoAsync(int photoId)
        {
            var photo = await _db.Photos.FirstOrDefaultAsync(p => p.Id == photoId);
            if (photo == null) throw new Exception("Photo not found");
            await _storageProvider.DeleteAsync(photo.GoogleDriveFileId);
            _db.Photos.Remove(photo);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteUserPhotosAsync(int userId)
        {
            var userPhotos = await _db.Photos.Where(p => p.UserId == userId).ToListAsync();
            foreach (var photo in userPhotos)
            {
                if (!string.IsNullOrEmpty(photo.GoogleDriveFileId))
                {
                    await _storageProvider.DeleteAsync(photo.GoogleDriveFileId);
                }
                _db.Photos.Remove(photo);
            }
            await _db.SaveChangesAsync();
        }

        public async Task DeletePetPhotosAsync(int petId)
        {
            var petPhotos = await _db.Photos.Where(p => p.PetId == petId).ToListAsync();
            foreach (var photo in petPhotos)
            {
                if (!string.IsNullOrEmpty(photo.GoogleDriveFileId))
                {
                    await _storageProvider.DeleteAsync(photo.GoogleDriveFileId);
                }
                _db.Photos.Remove(photo);
            }
            await _db.SaveChangesAsync();
        }
    }
} 