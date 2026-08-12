namespace Application.Interfaces;

/// <summary>
/// Exposes the identity of the caller for the current request, so the
/// Application layer never has to reference HttpContext directly.
/// </summary>
public interface ICurrentUserService
{
    string UserName { get; }

    string? UserId { get; }
}
