using PawMatch.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using PawMatch.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PawMatch.Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Logging;
using PawMatch.Application.DTOs;

namespace PawMatch.Application.Services
{
    public class DiscoverService : IDiscoverService
    {
        private readonly AppDbContext _db;
        private readonly IUserSwipeRepository _userSwipeRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DiscoverService> _logger;

        public DiscoverService(AppDbContext db, IUserSwipeRepository userSwipeRepository, IConfiguration configuration, ILogger<DiscoverService> logger)
        {
            _db = db;
            _userSwipeRepository = userSwipeRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<DiscoverUserPetDto>> DiscoverUsersAsync(int currentUserId, double? maxDistanceKm = null, string? preferredPetType = null, int? offset = null, int? limit = null)
        {
            // Configure exclusion and re-appearance durations from appsettings.json
            var swipeExclusionDurationDays = _configuration.GetValue<int?>("AppSettings:SwipeExclusionDurationDays") ?? 30; // Not directly used in the new logic but kept for consistency
            var swipeReappearDurationDays = _configuration.GetValue<int?>("AppSettings:SwipeReappearDurationDays") ?? 90;

            var reappearDate = DateTime.UtcNow.AddDays(-swipeReappearDurationDays);
            _logger.LogInformation($"DiscoverService: ReappearDate calculated as {reappearDate.ToShortDateString()}");

            // Get users that the current user has explicitly liked (IsLiked = true)
            // These users should generally be permanently excluded from discovery after the initial swipe,
            // unless a match is later unconfirmed. For simplicity, we exclude them.
            var likedUserIds = await _db.UserSwipes
                .Where(us => us.SwiperId == currentUserId && us.IsLiked)
                .Select(us => us.SwipedUserId)
                .ToListAsync();

            // Get users that the current user has passed (IsLiked = false) within the re-appearance duration.
            // Users passed before 'reappearDate' will be included in discovery again.
            var passedRecentlyUserIds = await _db.UserSwipes
                .Where(us => us.SwiperId == currentUserId && !us.IsLiked && us.SwipeDate >= reappearDate)
                .Select(us => us.SwipedUserId)
                                    .ToListAsync();

            // Combine both sets of excluded users: liked users (always excluded) and recently passed users.
            var excludedUserIds = likedUserIds.Union(passedRecentlyUserIds).ToList();
            _logger.LogInformation($"DiscoverService: Excluded User IDs: {string.Join(", ", excludedUserIds)}");

            // Start with all users except the current one and already excluded ones
            var query = _db.Users
                .Where(u => u.Id != currentUserId && !excludedUserIds.Contains(u.Id));

            // Apply location-based filtering (simplified for now, full PostGIS integration would be here)
            if (maxDistanceKm.HasValue)
            {
                // TODO: Implement actual geographical distance calculation with PostGIS
                // For now, this is a placeholder and doesn't actively filter by distance.
                // It ensures the parameter is used, but the logic would need PostGIS functions.
                // Example: query = query.Where(u => u.Latitude.HasValue && u.Longitude.HasValue &&
                //                                 _db.Database.ExecuteSqlRaw($"ST_DWithin(ST_SetSRID(ST_MakePoint({u.Longitude}, {u.Latitude}), 4326), ST_SetSRID(ST_MakePoint({currentUserLongitude}, {currentUserLatitude}), 4326), {maxDistanceKm * 1000})"));
            }

            // Apply pet type filtering
            if (!string.IsNullOrEmpty(preferredPetType))
            {
                // Filter users who have pets of the preferred type
                query = query.Where(u => u.Pets.Any(p => p.Type.ToLower() == preferredPetType.ToLower()));
            }

            var discoveredUserIds = await query.Select(u => u.Id).ToListAsync();

            // Kullanıcıları ve petlerini çek
            var usersWithDetails = await _db.Users
                .Where(u => discoveredUserIds.Contains(u.Id))
                .Include(u => u.Photos)
                .Include(u => u.Pets)
                    .ThenInclude(p => p.Photos)
                .ToListAsync(); // Verileri belleğe çek

            // Sayfalama uygula (bellekte)
            if (offset.HasValue)
            {
                usersWithDetails = usersWithDetails.Skip(offset.Value).ToList();
            }
            if (limit.HasValue)
            {
                usersWithDetails = usersWithDetails.Take(limit.Value).ToList();
            }

            var result = usersWithDetails.Select(u =>
            {
                var pet = u.Pets.FirstOrDefault();
                var photoIds = u.Photos.Select(p => p.Id).ToList();
                var petIds = u.Pets.Select(p => p.Id).ToList();
                return new DiscoverUserPetDto
                {
                    User = UserPublicDtoMapper.ToPublicDto(u, photoIds, petIds),
                    Pet = pet == null ? null : new DiscoverPetDto
                    {
                        Name = pet.Name,
                        Type = pet.Type,
                        Age = pet.Age,
                        Photos = pet.Photos.Select(pp => new DiscoverPhotoDto { GoogleDriveFileId = pp.GoogleDriveFileId }).ToList()
                    }
                };
            }).ToList();

            return result;
        }
    }
} 