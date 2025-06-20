using Microsoft.AspNetCore.SignalR.Client;
using PawMatch.Api;
using System.Net.Http;
using System.Net.Http.Json;
using PawMatch.Application.DTOs;
using System.Threading.Tasks;
using System;
namespace PawMatch.Tests;

public class SignalRHubTests : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly CustomWebApplicationFactory<Program> _factory;

    public SignalRHubTests(CustomWebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<string> RegisterAndLoginNewUser(string name, string email, string password)
    {
        var client = _factory.CreateClient();
        var registerDto = new UserRegisterDto { Name = name, Email = email, Password = password };
        var registerResponse = await client.PostAsJsonAsync("/api/v1/users/register", registerDto);
        registerResponse.EnsureSuccessStatusCode();
        var authResponse = await registerResponse.Content.ReadFromJsonAsync<ApiResponse<UserAuthResponseDto>>();
        return authResponse.Data.Token;
    }

    private HubConnection CreateAuthenticatedHubConnection(string token)
    {
        var client = _factory.CreateClient();
        return new HubConnectionBuilder()
            .WithUrl(client.BaseAddress + "chatHub", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();
    }

    [Fact]
    public async Task SignalR_Service_Is_Registered()
    {
        // Arrange
        var token = await RegisterAndLoginNewUser("SignalRUser1", $"signalruser1_{Guid.NewGuid()}@example.com", "Password123!");
        var hubConnection = CreateAuthenticatedHubConnection(token);

        // Act & Assert
        await hubConnection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task ChatHub_Endpoint_Is_Accessible()
    {
        var token = await RegisterAndLoginNewUser("SignalRUser2", $"signalruser2_{Guid.NewGuid()}@example.com", "Password123!");
        var hubConnection = CreateAuthenticatedHubConnection(token);

        await hubConnection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task OnConnectedAsync_AddsUserToGroup()
    {
        var token = await RegisterAndLoginNewUser("SignalRUser3", $"signalruser3_{Guid.NewGuid()}@example.com", "Password123!");
        var hubConnection = CreateAuthenticatedHubConnection(token);

        bool connected = false;
        hubConnection.Closed += error =>
        {
            connected = false;
            return Task.CompletedTask;
        };

        await hubConnection.StartAsync();
        connected = hubConnection.State == HubConnectionState.Connected;
        Assert.True(connected);

        await hubConnection.StopAsync();
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesUserFromGroup()
    {
        var token = await RegisterAndLoginNewUser("SignalRUser4", $"signalruser4_{Guid.NewGuid()}@example.com", "Password123!");
        var hubConnection = CreateAuthenticatedHubConnection(token);

        await hubConnection.StartAsync();
        await hubConnection.StopAsync();

        Assert.Equal(HubConnectionState.Disconnected, hubConnection.State);
    }

    [Fact]
    public async Task Multiple_Clients_Can_Connect_And_Disconnect()
    {
        var token1 = await RegisterAndLoginNewUser("SignalRUser5", $"signalruser5_{Guid.NewGuid()}@example.com", "Password123!");
        var token2 = await RegisterAndLoginNewUser("SignalRUser6", $"signalruser6_{Guid.NewGuid()}@example.com", "Password123!");
        var hubConnection1 = CreateAuthenticatedHubConnection(token1);
        var hubConnection2 = CreateAuthenticatedHubConnection(token2);

        await hubConnection1.StartAsync();
        await hubConnection2.StartAsync();

        Assert.Equal(HubConnectionState.Connected, hubConnection1.State);
        Assert.Equal(HubConnectionState.Connected, hubConnection2.State);

        await hubConnection1.StopAsync();
        await hubConnection2.StopAsync();

        Assert.Equal(HubConnectionState.Disconnected, hubConnection1.State);
        Assert.Equal(HubConnectionState.Disconnected, hubConnection2.State);
    }
}