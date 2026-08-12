using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using FluentAssertions;
using Xunit;

namespace API.Tests;

// AuthControllerTests and ProductsControllerTests each spin up their own full
// WebApplicationFactory<Program> host (running Program.cs for real, including its
// Serilog file sink writing to the same relative "logs/log-.txt" path). Putting both
// classes in the same named xUnit collection is what actually stops xUnit from
// running them at the same time - by default, tests within a single collection run
// sequentially, while different collections run in parallel with each other. Two
// hosts starting up concurrently were occasionally racing over a shared resource and
// crashing one host's startup ("entry point exited without ever building an IHost").
[Collection("SequentialApiHosts")]
public class AuthControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokenPair()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto
        {
            Username = "admin",
            Password = "Passw0rd!"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponseDto>();
        tokens.Should().NotBeNull();
        tokens!.AccessToken.Should().NotBeNullOrWhiteSpace();
        tokens.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto
        {
            Username = "admin",
            Password = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_WithValidTokens_ReturnsNewTokenPair()
    {
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequestDto
        {
            Username = "admin",
            Password = "Passw0rd!"
        });
        var firstTokens = await loginResponse.Content.ReadFromJsonAsync<TokenResponseDto>();

        var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequestDto
        {
            AccessToken = firstTokens!.AccessToken,
            RefreshToken = firstTokens.RefreshToken
        });

        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondTokens = await refreshResponse.Content.ReadFromJsonAsync<TokenResponseDto>();
        secondTokens.Should().NotBeNull();
        secondTokens!.RefreshToken.Should().NotBe(firstTokens.RefreshToken);
    }
}
