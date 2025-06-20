using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Threading.Tasks;
using PawMatch.Api;
using System.Net.Http;
using System.Net.Http.Json;
using PawMatch.Application.DTOs;
using System.Text.Json;
using System.Net;
using PawMatch.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using PawMatch.Domain;
using Moq;
using PawMatch.Application.Interfaces;
using PawMatch.Application.Services;
using PawMatch.Infrastructure.Repositories;
using PawMatch.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace PawMatch.Tests
{
    public class MatchesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public MatchesControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private async Task<(string token, int userId)> AuthenticateUser(string email, string password)
        {
            var loginDto = new UserLoginDto { Email = email, Password = password };
            var loginResponse = await _client.PostAsJsonAsync("/api/v1/users/login", loginDto);
            loginResponse.EnsureSuccessStatusCode();
            var authResponse = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
            return (authResponse.Data.Token, authResponse.Data.UserPrivate.Id);
        }

        private async Task<(string token, int userId)> RegisterAndLoginNewUser(string name, string email, string password)
        {
            var registerDto = new UserRegisterDto { Name = name, Email = email, Password = password };
            var registerResponse = await _client.PostAsJsonAsync("/api/v1/users/register", registerDto);
            registerResponse.EnsureSuccessStatusCode();
            var authResponse = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
            return (authResponse.Data.Token, authResponse.Data.UserPrivate.Id);
        }

        [Fact]
        public async Task LikeOrPass_SuccessfulLikeAndReciprocalMatch_ReturnsConfirmedTrue()
        {
            // Arrange
            var (user1Token, user1Id) = await RegisterAndLoginNewUser("TestUser1", "test1@example.com", "Password123!");
            var (user2Token, user2Id) = await RegisterAndLoginNewUser("TestUser2", "test2@example.com", "Password123!");

            // User2 likes User1 (reciprocal swipe)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user2Token);
            var user2LikesUser1Dto = new MatchActionDto { User1Id = user2Id, User2Id = user1Id, Liked = true };
            await _client.PostAsJsonAsync("/api/v1/matches", user2LikesUser1Dto);

            // Act: User1 likes User2
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Token);
            var user1LikesUser2Dto = new MatchActionDto { User1Id = user1Id, User2Id = user2Id, Liked = true };
            var response = await _client.PostAsJsonAsync("/api/v1/matches", user1LikesUser2Dto);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<MatchResultDto>>();
            Assert.NotNull(result);
            Assert.True(result.Data.Confirmed);
            Assert.True(result.Data.MatchId > 0);

            // Verify match in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var match = await dbContext.Matches.FirstOrDefaultAsync(m => (m.User1Id == user1Id && m.User2Id == user2Id) || (m.User1Id == user2Id && m.User2Id == user1Id));
                Assert.NotNull(match);
                Assert.True(match.Confirmed);
            }
        }

        [Fact]
        public async Task LikeOrPass_SuccessfulLikeNoReciprocalMatch_ReturnsConfirmedFalse()
        {
            // Arrange
            var (user1Token, user1Id) = await RegisterAndLoginNewUser("TestUser3", "test3@example.com", "Password123!");
            var (user2Token, user2Id) = await RegisterAndLoginNewUser("TestUser4", "test4@example.com", "Password123!");

            // Act: User1 likes User2 (no reciprocal like yet)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Token);
            var user1LikesUser2Dto = new MatchActionDto { User1Id = user1Id, User2Id = user2Id, Liked = true };
            var response = await _client.PostAsJsonAsync("/api/v1/matches", user1LikesUser2Dto);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<MatchResultDto>>();
            Assert.NotNull(result);
            Assert.False(result.Data.Confirmed);
            Assert.Equal(0, result.Data.MatchId); // No match created yet

            // Verify no match in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var match = await dbContext.Matches.FirstOrDefaultAsync(m => (m.User1Id == user1Id && m.User2Id == user2Id) || (m.User1Id == user2Id && m.User2Id == user1Id));
                Assert.Null(match);
            }
        }

        [Fact]
        public async Task LikeOrPass_SuccessfulPass_ReturnsConfirmedFalseAndUpdatesExistingMatch()
        {
            // Arrange
            var (user1Token, user1Id) = await RegisterAndLoginNewUser("TestUser5", "test5@example.com", "Password123!");
            var (user2Token, user2Id) = await RegisterAndLoginNewUser("TestUser6", "test6@example.com", "Password123!");

            // Ensure a confirmed match exists first
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await dbContext.Matches.AddAsync(new Domain.Match { User1Id = user1Id, User2Id = user2Id, Confirmed = true });
                await dbContext.SaveChangesAsync();
            }

            // Act: User1 passes User2
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Token);
            var user1PassesUser2Dto = new MatchActionDto { User1Id = user1Id, User2Id = user2Id, Liked = false };
            var response = await _client.PostAsJsonAsync("/api/v1/matches", user1PassesUser2Dto);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<MatchResultDto>>();
            Assert.NotNull(result);
            Assert.False(result.Data.Confirmed);

            // Verify match is unconfirmed in DB
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var match = await dbContext.Matches.FirstOrDefaultAsync(m => (m.User1Id == user1Id && m.User2Id == user2Id) || (m.User1Id == user2Id && m.User2Id == user1Id));
                Assert.NotNull(match);
                Assert.False(match.Confirmed);
            }
        }

        [Fact]
        public async Task LikeOrPass_InvalidUser1Id_ReturnsUnauthorized()
        {
            // Arrange
            var (user1Token, user1Id) = await RegisterAndLoginNewUser("TestUser7", "test7@example.com", "Password123!");
            var (_, user2Id) = await RegisterAndLoginNewUser("TestUser8", "test8@example.com", "Password123!");

            // Act: User1 tries to act on behalf of a different user (user2Id)
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Token);
            var invalidActionDto = new MatchActionDto { User1Id = user2Id, User2Id = user1Id, Liked = true }; // Invalid User1Id
            var response = await _client.PostAsJsonAsync("/api/v1/matches", invalidActionDto);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LikeOrPassAsync_ConfirmedMatch_CallsSendMatchNotificationAsyncForBothUsers()
        {
            // Arrange
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(dbContextOptions);
            var userSwipeRepo = new UserSwipeRepository(db);
            var mockNotification = new Mock<IRealtimeNotificationService>();
            var mockLogger = new Mock<ILogger<MatchService>>();
            var mockMatchRepo = new Mock<IMatchRepository>();
            var matchService = new MatchService(db, userSwipeRepo, mockNotification.Object, mockLogger.Object, mockMatchRepo.Object);
            // Kullanıcılar ve reciprocal swipe oluştur
            var user1Id = 1;
            var user2Id = 2;
            db.Users.Add(new User { Id = user1Id, Name = "User1", Email = "user1@example.com", PasswordHash = "hashed" });
            db.Users.Add(new User { Id = user2Id, Name = "User2", Email = "user2@example.com", PasswordHash = "hashed" });
            db.SaveChanges();
            await userSwipeRepo.AddAsync(new UserSwipe { SwiperId = user2Id, SwipedUserId = user1Id, IsLiked = true, SwipeDate = DateTime.UtcNow });

            var dto = new MatchActionDto { User1Id = user1Id, User2Id = user2Id, Liked = true };

            // Act
            var result = await matchService.LikeOrPassAsync(user1Id, dto);

            // Assert
            Assert.True(result.Confirmed);
            mockNotification.Verify(m => m.SendMatchNotificationAsync(user1Id, user2Id, It.Is<MatchResultDto>(r => r.Confirmed)), Times.Once);
        }

        [Fact]
        public async Task LikeOrPassAsync_NoReciprocalMatch_DoesNotCallSendMatchNotificationAsync()
        {
            // Arrange
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(dbContextOptions);
            var userSwipeRepo = new UserSwipeRepository(db);
            var mockNotification = new Mock<IRealtimeNotificationService>();
            var mockLogger = new Mock<ILogger<MatchService>>();
            var mockMatchRepo = new Mock<IMatchRepository>();
            var matchService = new MatchService(db, userSwipeRepo, mockNotification.Object, mockLogger.Object, mockMatchRepo.Object);
            var user1Id = 1;
            var user2Id = 2;
            db.Users.Add(new User { Id = user1Id, Name = "User1", Email = "user1@example.com", PasswordHash = "hashed" });
            db.Users.Add(new User { Id = user2Id, Name = "User2", Email = "user2@example.com", PasswordHash = "hashed" });
            db.SaveChanges();
            // reciprocal swipe yok

            var dto = new MatchActionDto { User1Id = user1Id, User2Id = user2Id, Liked = true };

            // Act
            var result = await matchService.LikeOrPassAsync(user1Id, dto);

            // Assert
            Assert.False(result.Confirmed);
            mockNotification.Verify(m => m.SendMatchNotificationAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<MatchResultDto>()), Times.Never);
        }
    }

    public class MessageServiceNotificationTests
    {
        [Fact]
        public async Task SendMessageAsync_ValidMessage_CallsSendMessageNotificationAndSavesToDb()
        {
            // Arrange
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(dbContextOptions);
            var messageRepo = new MessageRepository(db);
            var mockNotification = new Mock<IRealtimeNotificationService>();
            var mockUserRepo = new Mock<IUserRepository>();
            var messageService = new MessageService(messageRepo, mockNotification.Object, mockUserRepo.Object);

            // Test users
            var senderId = 1;
            var recipientId = 2;
            var sender = new User { Id = senderId, Name = "Sender", Email = "sender@example.com", PasswordHash = "hashed" };
            var recipient = new User { Id = recipientId, Name = "Recipient", Email = "recipient@example.com", PasswordHash = "hashed" };
            db.Users.Add(sender);
            db.Users.Add(recipient);
            db.SaveChanges();
            mockUserRepo.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync(sender);
            mockUserRepo.Setup(r => r.GetByIdAsync(recipientId)).ReturnsAsync(recipient);

            var content = "Merhaba!";

            // Act
            var message = await messageService.SendMessageAsync(senderId, recipientId, content);

            // Assert
            Assert.NotNull(message);
            Assert.Equal(senderId, message.SenderId);
            Assert.Equal(recipientId, message.RecipientId);
            Assert.Equal(content, message.Content);
            mockNotification.Verify(m => m.SendMessageAsync(recipientId, It.Is<Message>(msg => msg.Content == content && msg.SenderId == senderId)), Times.Once);

            // Mesaj veritabanına kaydedildi mi?
            var dbMessage = db.Messages.FirstOrDefault(m => m.SenderId == senderId && m.RecipientId == recipientId && m.Content == content);
            Assert.NotNull(dbMessage);
        }

        [Fact]
        public async Task SendMessageAsync_InvalidRecipient_ThrowsArgumentException()
        {
            // Arrange
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(dbContextOptions);
            var messageRepo = new MessageRepository(db);
            var mockNotification = new Mock<IRealtimeNotificationService>();
            var mockUserRepo = new Mock<IUserRepository>();
            var messageService = new MessageService(messageRepo, mockNotification.Object, mockUserRepo.Object);

            var senderId = 1;
            var recipientId = 999; // Geçersiz
            var sender = new User { Id = senderId, Name = "Sender", Email = "sender@example.com", PasswordHash = "hashed" };
            db.Users.Add(sender);
            db.SaveChanges();
            mockUserRepo.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync(sender);
            mockUserRepo.Setup(r => r.GetByIdAsync(recipientId)).ReturnsAsync((User)null);

            var content = "Geçersiz alıcıya mesaj";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => messageService.SendMessageAsync(senderId, recipientId, content));
        }

        [Fact]
        public async Task SendMessageAsync_InvalidSender_ThrowsArgumentException()
        {
            // Arrange
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            using var db = new AppDbContext(dbContextOptions);
            var messageRepo = new MessageRepository(db);
            var mockNotification = new Mock<IRealtimeNotificationService>();
            var mockUserRepo = new Mock<IUserRepository>();
            var messageService = new MessageService(messageRepo, mockNotification.Object, mockUserRepo.Object);

            var senderId = 1; // Geçersiz
            var recipientId = 2;
            var recipient = new User { Id = recipientId, Name = "Recipient", Email = "recipient@example.com", PasswordHash = "hashed" };
            db.Users.Add(recipient);
            db.SaveChanges();
            mockUserRepo.Setup(r => r.GetByIdAsync(senderId)).ReturnsAsync((User)null);
            mockUserRepo.Setup(r => r.GetByIdAsync(recipientId)).ReturnsAsync(recipient);

            var content = "Yetkisiz gönderici";

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => messageService.SendMessageAsync(senderId, recipientId, content));
        }
    }
} 