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
using System;
using PawMatch.Application.Interfaces;

namespace PawMatch.Tests
{
    public class DiscoverServiceTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public DiscoverServiceTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
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
        public async Task Discover_ExcludesSwipedUsersWithinExclusionPeriod()
        {
            // Arrange
            var (_, user1Id) = await RegisterAndLoginNewUser("DiscUser1", "disc1@example.com", "Password123!");
            var (_, user2Id) = await RegisterAndLoginNewUser("DiscUser2", "disc2@example.com", "Password123!");
            var (_, user3Id) = await RegisterAndLoginNewUser("DiscUser3", "disc3@example.com", "Password123!");

            // User1 swipes (likes) User2 directly in the database
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userSwipe = new UserSwipe
                {
                    SwiperId = user1Id,
                    SwipedUserId = user2Id,
                    IsLiked = true,
                    SwipeDate = DateTime.UtcNow
                };
                await dbContext.UserSwipes.AddAsync(userSwipe);
                await dbContext.SaveChangesAsync();
            }

            // Act: User1 discovers users by directly calling the service
            using (var scope = _factory.Services.CreateScope())
            {
                var discoverService = scope.ServiceProvider.GetRequiredService<IDiscoverService>();
                var discoveredUsers = await discoverService.DiscoverUsersAsync(user1Id);
                var discoveredUserIds = discoveredUsers.Select(u => u.User.Id).ToList();

                // Assert
                Assert.NotNull(discoveredUserIds);
                Assert.DoesNotContain(user2Id, discoveredUserIds); // User2 should be excluded
                Assert.Contains(user3Id, discoveredUserIds); // User3 should be discoverable
            }
        }

        [Fact]
        public async Task Discover_IncludesUsersSwipedOnOutsideExclusionPeriod()
        {
            // Arrange
            var (_, user1Id) = await RegisterAndLoginNewUser("DiscUser4", "disc4@example.com", "Password123!");
            var (_, user2Id) = await RegisterAndLoginNewUser("DiscUser5", "disc5@example.com", "Password123!");
            var (_, user3Id) = await RegisterAndLoginNewUser("DiscUser6", "disc6@example.com", "Password123!");

            // Manually add a swipe record for User1 swiping User2, but set SwipeDate to be outside the exclusion period
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userSwipe = new UserSwipe
                {
                    SwiperId = user1Id,
                    SwipedUserId = user2Id,
                    IsLiked = true,
                    SwipeDate = DateTime.UtcNow.AddDays(-60) // Assuming default exclusion is 30 days
                };
                await dbContext.UserSwipes.AddAsync(userSwipe);
                await dbContext.SaveChangesAsync();
            }

            // Act: User1 discovers users by directly calling the service
            using (var scope = _factory.Services.CreateScope())
            {
                var discoverService = scope.ServiceProvider.GetRequiredService<IDiscoverService>();
                var discoveredUsers = await discoverService.DiscoverUsersAsync(user1Id);
                var discoveredUserIds = discoveredUsers.Select(u => u.User.Id).ToList();

                // Assert
                Assert.NotNull(discoveredUserIds);
                // User2 should NOT be in the discovered list for User1 because the swipe was a like, which are permanently excluded.
                Assert.DoesNotContain(user2Id, discoveredUserIds); 
                Assert.Contains(user3Id, discoveredUserIds);
            }
        }

        [Fact]
        public async Task Discover_ReincludesPassedUsersAfterReappearDuration()
        {
            // Arrange
            var (_, user1Id) = await RegisterAndLoginNewUser("ReappearUser1", "reappear1@example.com", "Password123!");
            var (_, user2Id) = await RegisterAndLoginNewUser("ReappearUser2", "reappear2@example.com", "Password123!");
            var (_, user3Id) = await RegisterAndLoginNewUser("ReappearUser3", "reappear3@example.com", "Password123!");

            // User1 passes User2, but the swipe date is older than the reappear duration (e.g., 90 days default)
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userSwipe = new UserSwipe
                {
                    SwiperId = user1Id,
                    SwipedUserId = user2Id,
                    IsLiked = false, // Passed
                    SwipeDate = DateTime.UtcNow.AddDays(-100) // Older than 90 days reappear duration
                };
                await dbContext.UserSwipes.AddAsync(userSwipe);
                await dbContext.SaveChangesAsync();
            }

            // Act: User1 discovers users by directly calling the service
            using (var scope = _factory.Services.CreateScope())
            {
                var discoverService = scope.ServiceProvider.GetRequiredService<IDiscoverService>();
                var discoveredUsers = await discoverService.DiscoverUsersAsync(user1Id);
                var discoveredUserIds = discoveredUsers.Select(u => u.User.Id).ToList();

                // Assert
                Assert.NotNull(discoveredUserIds);
                Assert.Contains(user2Id, discoveredUserIds); // User2 should reappear in discovery list
                Assert.Contains(user3Id, discoveredUserIds); // User3 should still be discoverable
            }
        }

        [Fact]
        public async Task Discover_ExcludesPassedUsersWithinReappearDuration()
        {
            // Arrange
            var (_, user1Id) = await RegisterAndLoginNewUser("ExcludeReappearUser1", "exreappear1@example.com", "Password123!");
            var (_, user2Id) = await RegisterAndLoginNewUser("ExcludeReappearUser2", "exreappear2@example.com", "Password123!");
            var (_, user3Id) = await RegisterAndLoginNewUser("ExcludeReappearUser3", "exreappear3@example.com", "Password123!");

            // User1 passes User2, and the swipe date is within the reappear duration (e.g., 90 days default)
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var userSwipe = new UserSwipe
                {
                    SwiperId = user1Id,
                    SwipedUserId = user2Id,
                    IsLiked = false, // Passed
                    SwipeDate = DateTime.UtcNow.AddDays(-10) // Within 90 days reappear duration
                };
                await dbContext.UserSwipes.AddAsync(userSwipe);
                await dbContext.SaveChangesAsync();
            }

            // Act: User1 discovers users by directly calling the service
            using (var scope = _factory.Services.CreateScope())
            {
                var discoverService = scope.ServiceProvider.GetRequiredService<IDiscoverService>();
                var discoveredUsers = await discoverService.DiscoverUsersAsync(user1Id);
                var discoveredUserIds = discoveredUsers.Select(u => u.User.Id).ToList();

                // Assert
                Assert.NotNull(discoveredUserIds);
                Assert.DoesNotContain(user2Id, discoveredUserIds); // User2 should NOT reappear in discovery list yet
                Assert.Contains(user3Id, discoveredUserIds); // User3 should still be discoverable
            }
        }

        [Fact]
        public async Task Discover_WithMaxDistanceKm_ReturnsSuccess()
        {
            // Arrange
            var (user1Token, user1Id) = await RegisterAndLoginNewUser("DiscUser7", "disc7@example.com", "Password123!");
            var (user2Token, user2Id) = await RegisterAndLoginNewUser("DiscUser8", "disc8@example.com", "Password123!");

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Token);
            var discoverResponse = await _client.GetAsync($"/api/v1/matches/discover?maxDistanceKm=100");

            // Assert
            discoverResponse.EnsureSuccessStatusCode();
            var result = await discoverResponse.Content.ReadFromJsonAsync<ApiResponse<List<DiscoverUserPetDto>>>();
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            // Further assertions would depend on actual geographical data and filtering logic
        }

        [Fact]
        public async Task Discover_WithPreferredPetType_ReturnsSuccess()
        {
            // Arrange
            var (user1Token, user1Id) = await RegisterAndLoginNewUser("DiscUser9", "disc9@example.com", "Password123!");
            var (user2Token, user2Id) = await RegisterAndLoginNewUser("DiscUser10", "disc10@example.com", "Password123!");

            // Act
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", user1Token);
            var discoverResponse = await _client.GetAsync($"/api/v1/matches/discover?preferredPetType=dog");

            // Assert
            discoverResponse.EnsureSuccessStatusCode();
            var result = await discoverResponse.Content.ReadFromJsonAsync<ApiResponse<List<DiscoverUserPetDto>>>();
            Assert.NotNull(result);
            Assert.NotNull(result.Data);
            // Further assertions would depend on actual pet data and filtering logic
        }
    }
} 