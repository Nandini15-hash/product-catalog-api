using System.Security.Claims;

namespace Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(string userId, string userName, IEnumerable<string> roles);

    string GenerateRefreshToken();

    /// <summary>
    /// Reads the claims out of an (possibly expired) access token without
    /// validating its lifetime - used by the refresh-token flow.
    /// </summary>
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
