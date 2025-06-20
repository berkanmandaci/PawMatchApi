using System.Collections.Generic;
using System.Threading.Tasks;
using PawMatch.Application.DTOs;

namespace PawMatch.Application.Interfaces
{
    public interface IDiscoverService
    {
        Task<List<DiscoverUserPetDto>> DiscoverUsersAsync(int currentUserId, double? maxDistanceKm = null, string? preferredPetType = null, int? offset = null, int? limit = null);
    }
} 