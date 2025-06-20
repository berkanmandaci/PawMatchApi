using System.Collections.Generic;
using System.Threading.Tasks;
using PawMatch.Application.DTOs;
using PawMatch.Application.Interfaces;
using PawMatch.Infrastructure;
using Microsoft.EntityFrameworkCore;
using PawMatch.Infrastructure.Interfaces;
using PawMatch.Domain;
using Microsoft.Extensions.Logging;

namespace PawMatch.Application.Services
{
    public class MatchService : IMatchService
    {
        private readonly AppDbContext _db;
        private readonly IUserSwipeRepository _userSwipeRepository;
        private readonly IRealtimeNotificationService _realtimeNotificationService;
        private readonly ILogger<MatchService> _logger;
        private readonly IMatchRepository _matchRepository;

        public MatchService(AppDbContext db, IUserSwipeRepository userSwipeRepository, IRealtimeNotificationService realtimeNotificationService, ILogger<MatchService> logger, IMatchRepository matchRepository)
        {
            _db = db;
            _userSwipeRepository = userSwipeRepository;
            _realtimeNotificationService = realtimeNotificationService;
            _logger = logger;
            _matchRepository = matchRepository;
        }

        public async Task<MatchResultDto> LikeOrPassAsync(int currentUserId, MatchActionDto dto)
        {
            if (currentUserId != dto.User1Id)
            {
                throw new UnauthorizedAccessException("Unauthorized: User ID mismatch.");
            }

            var userSwipe = new UserSwipe
            {
                SwiperId = dto.User1Id,
                SwipedUserId = dto.User2Id,
                IsLiked = dto.Liked,
                SwipeDate = DateTime.UtcNow
            };
            await _userSwipeRepository.AddAsync(userSwipe);

            var matchResult = new MatchResultDto { MatchId = 0, Confirmed = false };

            if (dto.Liked)
            {
                var reciprocalSwipe = await _userSwipeRepository.GetBySwiperAndSwipedUserAsync(dto.User2Id, dto.User1Id);

                if (reciprocalSwipe != null && reciprocalSwipe.IsLiked)
                {
                    var existingMatch = await _db.Matches
                        .FirstOrDefaultAsync(m => (m.User1Id == dto.User1Id && m.User2Id == dto.User2Id) ||
                                                    (m.User1Id == dto.User2Id && m.User2Id == dto.User1Id));

                    if (existingMatch == null)
                    {
                        var newMatch = new Match
                        {
                            User1Id = dto.User1Id,
                            User2Id = dto.User2Id,
                            Confirmed = true
                        };
                        await _db.Matches.AddAsync(newMatch);
                        await _db.SaveChangesAsync();
                        matchResult.MatchId = newMatch.Id;
                        _logger.LogInformation($"Yeni eşleşme oluşturuldu: MatchId={newMatch.Id}, User1Id={dto.User1Id}, User2Id={dto.User2Id}");
                    }
                    else
                    {
                        existingMatch.Confirmed = true;
                        _db.Matches.Update(existingMatch);
                        await _db.SaveChangesAsync();
                        matchResult.MatchId = existingMatch.Id;
                        _logger.LogInformation($"Mevcut eşleşme güncellendi: MatchId={existingMatch.Id}, User1Id={dto.User1Id}, User2Id={dto.User2Id}");
                    }
                    matchResult.Confirmed = true;

                    try
                    {
                        _logger.LogInformation($"Notification gönderiliyor: User1Id={dto.User1Id}, User2Id={dto.User2Id}, MatchId={matchResult.MatchId}");
                    await _realtimeNotificationService.SendMatchNotificationAsync(dto.User1Id, dto.User2Id, matchResult);
                        _logger.LogInformation($"Notification başarıyla gönderildi: User1Id={dto.User1Id}, User2Id={dto.User2Id}, MatchId={matchResult.MatchId}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Notification gönderilemedi! User1Id={dto.User1Id}, User2Id={dto.User2Id}, MatchId={matchResult.MatchId}");
                    }
                }
            }
            else
            {
                var existingMatch = await _db.Matches
                    .FirstOrDefaultAsync(m => (m.User1Id == dto.User1Id && m.User2Id == dto.User2Id && m.Confirmed) ||
                                                (m.User1Id == dto.User2Id && m.User2Id == dto.User1Id && m.Confirmed));

                if (existingMatch != null)
                {
                    existingMatch.Confirmed = false;
                    _db.Matches.Update(existingMatch);
                    await _db.SaveChangesAsync();
                }
            }

            return matchResult;
        }

        /// <summary>
        /// Kullanıcının eşleşmelerini DTO olarak döndürür. Mapping sırasında User entity'den UserPublicDto'ya ve PhotoIds'e dönüştürülür.
        /// </summary>
        public async Task<List<MatchDto>> GetMatchesForUserAsync(int userId)
        {
            var matches = await _db.Matches
                .Where(m => m.Confirmed && (m.User1Id == userId || m.User2Id == userId))
                .Include(m => m.User1)
                .Include(m => m.User2)
                .ToListAsync();

            var result = new List<MatchDto>();
            foreach (var match in matches)
            {
                // Karşı tarafı bul
                var otherUser = match.User1Id == userId ? match.User2 : match.User1;
                var photoIds = await _db.Photos
                    .Where(p => p.UserId == otherUser.Id)
                    .Select(p => p.Id)
                    .ToListAsync();
                var petIds = await _db.Pets
                    .Where(p => p.UserId == otherUser.Id)
                    .Select(p => p.Id)
                    .ToListAsync();
                result.Add(new MatchDto
                {
                    MatchId = match.Id,
                    Confirmed = match.Confirmed,
                    User = UserPublicDtoMapper.ToPublicDto(otherUser, photoIds, petIds)
                });
            }
            return result;
        }

        public async Task<Match> GetMatchByIdAsync(int matchId)
        {
            return await _matchRepository.GetByIdAsync(matchId);
        }
    }
} 