using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawMatch.Application.DTOs;
using PawMatch.Application.Interfaces;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PawMatch.Api.Controllers
{
    [ApiController]
    [Route("api/v1/matches")]
    [Authorize]
    public class MatchesController : BaseController
    {
        private readonly IMatchService _matchService;
        private readonly IDiscoverService _discoverService;

        public MatchesController(IMatchService matchService, IDiscoverService discoverService)
        {
            _matchService = matchService;
            _discoverService = discoverService;
        }

        /// <summary>
        /// Kullanıcı/pet kartlarını listeler (discover).
        /// </summary>
        /// <param name="maxDistanceKm">Maksimum mesafe (opsiyonel)</param>
        /// <param name="offset">Sayfalama başlangıcı (opsiyonel)</param>
        /// <param name="limit">Sayfa boyutu (opsiyonel)</param>
        /// <param name="preferredPetType">Tercih edilen evcil hayvan türü (opsiyonel)</param>
        /// <returns>Kullanıcı ve pet kartları</returns>
        [HttpGet("discover")]
        public async Task<IActionResult> Discover([FromQuery] int? maxDistanceKm, [FromQuery] int? offset, [FromQuery] int? limit, [FromQuery] string? preferredPetType)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });
            
            var result = await _discoverService.DiscoverUsersAsync(userId.Value, maxDistanceKm, preferredPetType, offset, limit);
            return Ok(new { status = "success", data = result, error = (string)null });
        }

        /// <summary>
        /// Beğenme/geçme işlemi yapar.
        /// </summary>
        /// <param name="dto">Beğenme/geçme bilgisi</param>
        /// <returns>Eşleşme sonucu</returns>
        [HttpPost]
        public async Task<IActionResult> LikeOrPass([FromBody] MatchActionDto dto)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });

            try
            {
                var result = await _matchService.LikeOrPassAsync(userId.Value, dto);
                return Ok(new { status = "success", data = result, error = (string)null });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { status = "error", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMatches()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });
            var matches = await _matchService.GetMatchesForUserAsync(userId.Value);
            return Ok(new { data = new { matches }, status = "success" });
        }
    }
} 