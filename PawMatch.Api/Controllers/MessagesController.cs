using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PawMatch.Application.DTOs;
using PawMatch.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PawMatch.Domain;

namespace PawMatch.Api.Controllers
{
    [ApiController]
    [Route("api/v1/messages")]
    [Authorize]
    public class MessagesController : BaseController
    {
        private readonly IMessageService _messageService;
        private readonly IUserService _userService;
        private readonly IMatchService _matchService;

        public MessagesController(IMessageService messageService, IUserService userService, IMatchService matchService)
        {
            _messageService = messageService;
            _userService = userService;
            _matchService = matchService;
        }

        /// <summary>
        /// Belirli bir eşleşmeye ait mesajları listeler.
        /// </summary>
        /// <param name="matchId">Eşleşme ID</param>
        /// <param name="offset">Sayfalama başlangıcı (opsiyonel)</param>
        /// <param name="limit">Sayfa boyutu (opsiyonel, default 20)</param>
        /// <returns>Mesaj listesi</returns>
        [HttpGet("{matchId}")]
        public async Task<IActionResult> GetMessages(int matchId, [FromQuery] int offset = 0, [FromQuery] int limit = 20)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });

            // Kullanıcı bu eşleşmenin parçası mı kontrol et
            var match = await _matchService.GetMatchByIdAsync(matchId);
            if (match == null || (match.User1Id != userId && match.User2Id != userId))
                return Forbid();

            // Karşı tarafın userId'sini bul
            int otherUserId = (match.User1Id == userId) ? match.User2Id : match.User1Id;

            // Mesajları getir
            var messages = await _messageService.GetChatHistoryAsync(userId.Value, otherUserId);
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();
            var senders = await _userService.GetUsersByIdsAsync(senderIds);
            var senderMap = senders.ToDictionary(
                u => u.Id,
                u => UserPublicDtoMapper.ToPublicDto(
                    u,
                    u.GetPhotoIds(),
                    u.GetPetIds()
                )
            );
            var messageDtos = messages.Skip(offset).Take(limit).Select(m => MessageDtoMapper.ToDto(m, senderMap[m.SenderId])).ToList();
            return Ok(new { data = messageDtos, status = "success" });
        }

        /// <summary>
        /// Mesaj gönderir.
        /// </summary>
        /// <param name="request">Mesaj gönderme isteği</param>
        /// <returns>Oluşturulan mesaj</returns>
        public class SendMessageRequest
        {
            public int MatchId { get; set; }
            public string Content { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null)
                return Unauthorized(new { status = "error", error = "Kullanıcı kimliği doğrulanamadı." });

            // Kullanıcı bu eşleşmenin parçası mı kontrol et
            var match = await _matchService.GetMatchByIdAsync(request.MatchId);
            if (match == null || (match.User1Id != userId && match.User2Id != userId))
                return Forbid();

            int recipientId = (match.User1Id == userId) ? match.User2Id : match.User1Id;
            var message = await _messageService.SendMessageAsync(userId.Value, recipientId, request.Content);
            var sender = await _userService.GetUserByIdAsync(userId.Value);
            var senderDto = UserPublicDtoMapper.ToPublicDto(sender, sender.GetPhotoIds(),sender.GetPetIds());
            var messageDto = MessageDtoMapper.ToDto(message, senderDto);
            return Ok(new { data = messageDto, status = "success" });
        }
    }
} 