using System.Net.Http.Json;
using Xunit;
using System.Threading.Tasks;
using PawMatch.Api;
using PawMatch.Application.DTOs;
using System.Net.Http.Headers;

namespace PawMatch.Tests;

// Removed duplicated DTO definitions from here, they are now referenced from PawMatch.Application.DTOs

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ValidUser_ReturnsSuccessAndToken()
    {
        // Arrange
        var request = new UserRegisterDto
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "Password123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users/register", request);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();

        Assert.NotNull(apiResponse);
        Assert.Equal("success", apiResponse.Status);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.UserPrivate);
        Assert.NotNull(apiResponse.Data.Token);
        Assert.Equal(request.Email, apiResponse.Data.UserPrivate.Email);
        Assert.NotNull(apiResponse.Data.UserPrivate.PhotoIds);
        Assert.Empty(apiResponse.Data.UserPrivate.PhotoIds);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsSuccessAndToken()
    {
        // Arrange: First, register a user to ensure we have credentials to log in with
        var registerRequest = new UserRegisterDto
        {
            Name = "Login Test User",
            Email = "logintest@example.com",
            Password = "Password123!"
        };
        await _client.PostAsJsonAsync("/api/v1/users/register", registerRequest);

        var loginRequest = new UserLoginDto
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/users/login", loginRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();

        Assert.NotNull(apiResponse);
        Assert.Equal("success", apiResponse.Status);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.UserPrivate);
        Assert.NotNull(apiResponse.Data.Token);
        Assert.Equal(loginRequest.Email, apiResponse.Data.UserPrivate.Email);
        Assert.NotNull(apiResponse.Data.UserPrivate.PhotoIds);
        Assert.Empty(apiResponse.Data.UserPrivate.PhotoIds);
    }

    [Fact]
    public async Task UpdateProfile_ValidData_ReturnsSuccessAndUpdatedUser()
    {
        // Arrange: Register and log in a user to get a valid token
        var registerRequest = new UserRegisterDto
        {
            Name = "Profile Update User",
            Email = "profileupdate@example.com",
            Password = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/users/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registerApiResponse = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
        var authToken = registerApiResponse.Data.Token;

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        var updateRequest = new UpdateProfileDto
        {
            Name = "Updated Name",
            Bio = "Updated Bio",
            HasPet = true
        };

        // Act
        var response = await _client.PatchAsJsonAsync("/api/v1/users/profile", updateRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();

        Assert.NotNull(apiResponse);
        Assert.Equal("success", apiResponse.Status);
        Assert.NotNull(apiResponse.Data);
        Assert.NotNull(apiResponse.Data.UserPrivate);
        Assert.Equal(updateRequest.Name, apiResponse.Data.UserPrivate.Name);
        Assert.Equal(updateRequest.Bio, apiResponse.Data.UserPrivate.Bio);
        Assert.Equal(updateRequest.HasPet, apiResponse.Data.UserPrivate.HasPet);
        Assert.NotNull(apiResponse.Data.UserPrivate.PhotoIds);
        Assert.Empty(apiResponse.Data.UserPrivate.PhotoIds);

        // Clean up the Authorization header for subsequent tests
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task DeleteMyAccount_ValidUser_ReturnsSuccessAndDeletesAccount()
    {
        // Arrange: Register and log in a user to get a valid token
        var registerRequest = new UserRegisterDto
        {
            Name = "Delete User Test",
            Email = "deleteuser@example.com",
            Password = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/users/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registerApiResponse = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
        var authToken = registerApiResponse.Data.Token;

        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);

        // Act: Delete the user's account
        var deleteResponse = await _client.DeleteAsync("/api/v1/users/me");

        // Assert: Check if the deletion was successful
        deleteResponse.EnsureSuccessStatusCode(); // Expect 200 OK
        var deleteApiResponse = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.NotNull(deleteApiResponse);
        Assert.Equal("success", deleteApiResponse.Status);

        // Optional: Try to log in with the deleted user's credentials to confirm deletion
        _client.DefaultRequestHeaders.Authorization = null; // Clear header for next request

        var loginRequest = new UserLoginDto
        {
            Email = registerRequest.Email,
            Password = registerRequest.Password
        };

        var loginResponseAfterDelete = await _client.PostAsJsonAsync("/api/v1/users/login", loginRequest);

        // Assert: Expect Unauthorized (401) as the account should be deleted
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, loginResponseAfterDelete.StatusCode);
    }

    [Fact]
    public async Task GetUserProfile_ValidUser_ReturnsSuccessAndProfile()
    {
        // Arrange
        var client = _client;
        var registerDto = new UserRegisterDto { Name = "Test User", Email = "test.profile@example.com", Password = "Password123!" };
        var registerResponse = await client.PostAsJsonAsync("/api/v1/users/register", registerDto);
        registerResponse.EnsureSuccessStatusCode();

        var loginDto = new UserLoginDto { Email = "test.profile@example.com", Password = "Password123!" };
        var loginResponse = await client.PostAsJsonAsync("/api/v1/users/login", loginDto);
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResponse?.Data?.Token);

        // Act
        var profileResponse = await client.GetAsync("/api/v1/users/me");

        // Assert
        profileResponse.EnsureSuccessStatusCode();
        var apiResponse = await profileResponse.Content.ReadFromJsonAsync<ApiResponse<UserPrivateDto>>();
        Assert.NotNull(apiResponse);
        Assert.Equal("Success", apiResponse.Status);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(registerDto.Email, apiResponse.Data.Email);
        Assert.Equal(registerDto.Name, apiResponse.Data.Name);
        Assert.False(apiResponse.Data.HasPet);
        Assert.False(apiResponse.Data.HasProfile);
        Assert.NotNull(apiResponse.Data.PhotoIds);
        Assert.Empty(apiResponse.Data.PhotoIds);
    }
} 