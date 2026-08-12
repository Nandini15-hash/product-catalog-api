using System.Collections.Concurrent;
using Application.DTOs;
using Application.Interfaces;
using Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity;

/// <summary>
/// Minimal auth provider for demo/assessment purposes: a single seeded admin
/// user plus an in-memory refresh-token store. Swap this out for
/// ASP.NET Core Identity / an external IdP in a real deployment.
/// </summary>
public class AuthService : IAuthService
{
    private static readonly ConcurrentDictionary<string, (string UserId, string UserName, DateTime ExpiresAtUtc)> RefreshTokens = new();

    private readonly ITokenService _tokenService;
    private readonly JwtSettings _settings;

    // Seeded demo user - username: admin, password: Passw0rd!
    private const string SeedUserId = "1";
    private const string SeedUserName = "admin";
    private const string SeedPassword = "Passw0rd!";

    public AuthService(ITokenService tokenService, IOptions<JwtSettings> settings)
    {
        _tokenService = tokenService;
        _settings = settings.Value;
    }

    public Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.Username, SeedUserName, StringComparison.OrdinalIgnoreCase) ||
            request.Password != SeedPassword)
        {
            throw new UnauthorizedAppException("Invalid username or password.");
        }

        return Task.FromResult(IssueTokens(SeedUserId, SeedUserName));
    }

    public Task<TokenResponseDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default)
    {
        var principal = _tokenService.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal is null)
        {
            throw new UnauthorizedAppException("Invalid access token.");
        }

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userName = principal.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (userId is null || userName is null ||
            !RefreshTokens.TryGetValue(request.RefreshToken, out var stored) ||
            stored.UserId != userId ||
            stored.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new UnauthorizedAppException("Invalid or expired refresh token.");
        }

        RefreshTokens.TryRemove(request.RefreshToken, out _);

        return Task.FromResult(IssueTokens(userId, userName));
    }

    private TokenResponseDto IssueTokens(string userId, string userName)
    {
        var accessToken = _tokenService.GenerateAccessToken(userId, userName, new[] { "Admin" });
        var refreshToken = _tokenService.GenerateRefreshToken();
        var refreshExpiresAt = DateTime.UtcNow.AddDays(_settings.RefreshTokenExpirationDays);

        RefreshTokens[refreshToken] = (userId, userName, refreshExpiresAt);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.AccessTokenExpirationMinutes)
        };
    }
}
