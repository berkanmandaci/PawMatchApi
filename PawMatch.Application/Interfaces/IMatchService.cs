using System.Collections.Generic;
using System.Threading.Tasks;
using PawMatch.Application.DTOs;
using PawMatch.Domain;

namespace PawMatch.Application.Interfaces
{
    public interface IMatchService
    {
        Task<MatchResultDto> LikeOrPassAsync(int currentUserId, MatchActionDto dto);
        Task<List<MatchDto>> GetMatchesForUserAsync(int userId);
        Task<Match> GetMatchByIdAsync(int matchId);
    }
} 