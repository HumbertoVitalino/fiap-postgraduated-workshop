using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.LogoutUser.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.LogoutUser;

public sealed class LogoutUserUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IRefreshTokenService refreshTokenService,
    ILogger<LogoutUserUseCase> logger
) : ILogoutUserUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly ILogger<LogoutUserUseCase> _logger = logger;

    public async Task<Output> Handle(LogoutUserInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var tokenHash = _refreshTokenService.Hash(input.RefreshToken);
        var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (refreshToken is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] Refresh token not found.",
                input.CorrelationId
            );

            return output;
        }

        if (refreshToken.IsRevoked)
            return output;

        refreshToken.Revoke();
        _refreshTokenRepository.Update(refreshToken);

        var saved = await _refreshTokenRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] Error revoking refresh token {RefreshTokenId}.",
                input.CorrelationId,
                refreshToken.Id
            );

            output.AddErrorMessage("Error revoking refresh token.");
            return output;
        }

        return output;
    }
}
