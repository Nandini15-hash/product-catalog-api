using Application.DTOs;

namespace Application.Interfaces;

public interface IAuthService
{
    Task<TokenResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);

    Task<TokenResponseDto> RefreshAsync(RefreshRequestDto request, CancellationToken cancellationToken = default);
}
