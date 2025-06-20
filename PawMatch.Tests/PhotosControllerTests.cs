using System.Net.Http.Json;
using Xunit;
using System.Threading.Tasks;
using PawMatch.Api;
using PawMatch.Application.DTOs;
using System.Net.Http.Headers;
using System.IO;

namespace PawMatch.Tests;

public class PhotosControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PhotosControllerTests(CustomWebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task UploadUserPhoto_ValidFileAndAuth_ReturnsSuccess()
    {
        // Arrange: Register and log in a user
        var registerRequest = new UserRegisterDto
        {
            Name = "Photo Uploader",
            Email = "photoupload@example.com",
            Password = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/users/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registerApiResponse = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
        var authToken = registerApiResponse.Data.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        // Create a mock file (testphoto.jpg)
        var filePath = "testphoto.jpg";
        var fileContent = "This is a dummy image content for testing.";
        var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(memoryStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", filePath);

        // Act
        var response = await _client.PostAsync("/api/v1/photos/user", content);

        // Assert
        response.EnsureSuccessStatusCode(); // Expect 200 OK
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PhotoDto>>();

        Assert.NotNull(apiResponse);
        Assert.Equal("success", apiResponse.Status);
        Assert.NotNull(apiResponse.Data);
        Assert.Equal(filePath, apiResponse.Data.FileName);
        Assert.NotNull(apiResponse.Data.GoogleDriveFileId);

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task GetPhoto_ValidIdAndAuth_ReturnsPhotoStream()
    {
        // Arrange: Register, log in, and upload a photo
        var registerRequest = new UserRegisterDto
        {
            Name = "Photo Getter",
            Email = "photoget@example.com",
            Password = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/users/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registerApiResponse = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
        var authToken = registerApiResponse.Data.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        var filePath = "testphoto_get.jpg";
        var fileContent = "This is content for getting a photo.";
        var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(memoryStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", filePath);

        var uploadResponse = await _client.PostAsync("/api/v1/photos/user", content);
        uploadResponse.EnsureSuccessStatusCode();
        var uploadApiResponse = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<PhotoDto>>();
        var googleDriveFileId = uploadApiResponse.Data.GoogleDriveFileId;

        // Act: Get the photo stream
        var getResponse = await _client.GetAsync($"/api/v1/photos/{googleDriveFileId}");

        // Assert: Check if the response is successful and content matches
        getResponse.EnsureSuccessStatusCode();
        var returnedContent = await getResponse.Content.ReadAsStringAsync();
        Assert.Equal(fileContent, returnedContent);

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task DeletePhoto_ValidIdAndAuth_ReturnsSuccess()
    {
        // Arrange: Register, log in, and upload a photo
        var registerRequest = new UserRegisterDto
        {
            Name = "Photo Deleter",
            Email = "photodelete@example.com",
            Password = "Password123!"
        };
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/users/register", registerRequest);
        registerResponse.EnsureSuccessStatusCode();
        var registerApiResponse = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
        var authToken = registerApiResponse.Data.Token;

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authToken);

        var filePath = "testphoto_delete.jpg";
        var fileContent = "This is content for deleting a photo.";
        var memoryStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));

        var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(memoryStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Add(streamContent, "file", filePath);

        var uploadResponse = await _client.PostAsync("/api/v1/photos/user", content);
        uploadResponse.EnsureSuccessStatusCode();
        var uploadApiResponse = await uploadResponse.Content.ReadFromJsonAsync<ApiResponse<PhotoDto>>();
        var googleDriveFileId = uploadApiResponse.Data.GoogleDriveFileId; // Use GoogleDriveFileId for deletion

        // Act: Delete the photo
        var deleteResponse = await _client.DeleteAsync($"/api/v1/photos/{googleDriveFileId}");

        // Assert: Check if the deletion was successful
        deleteResponse.EnsureSuccessStatusCode(); // Expect 200 OK
        var deleteApiResponse = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse<object>>();

        Assert.NotNull(deleteApiResponse);
        Assert.Equal("success", deleteApiResponse.Status);

        // Optional: Try to get the photo again to confirm deletion (expect 404 Not Found)
        var getResponseAfterDelete = await _client.GetAsync($"/api/v1/photos/{googleDriveFileId}");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, getResponseAfterDelete.StatusCode); // Changed from Unauthorized to NotFound

        // Clean up
        _client.DefaultRequestHeaders.Authorization = null;
    }
} 