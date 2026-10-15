using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken;

public sealed class RefreshAccessTokenUseCase(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtService jwtService,
    IRefreshTokenService refreshTokenService,
    ILogger<RefreshAccessTokenUseCase> logger
) : IRefreshAccessTokenUseCase
{
    private readonly IRefreshTokenRepository _refreshTokenRepository = refreshTokenRepository;
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IRefreshTokenService _refreshTokenService = refreshTokenService;
    private readonly ILogger<RefreshAccessTokenUseCase> _logger = logger;

    public async Task<Output> Handle(RefreshAccessTokenInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var tokenHash = _refreshTokenService.Hash(input.RefreshToken);
        var currentRefreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (currentRefreshToken is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] Refresh token not found.",
                input.CorrelationId
            );

            output.AddErrorMessage("Refresh token not found.");
            return output;
        }

        if (currentRefreshToken.IsRevoked)
        {
            _logger.LogWarning(
                "[{CorrelationId}] Refresh token {RefreshTokenId} has already been revoked.",
                input.CorrelationId,
                currentRefreshToken.Id
            );

            output.AddErrorMessage("Refresh token has already been revoked.");
            return output;
        }

        if (!currentRefreshToken.IsActive())
        {
            _logger.LogWarning(
                "[{CorrelationId}] Refresh token {RefreshTokenId} has expired.",
                input.CorrelationId,
                currentRefreshToken.Id
            );

            output.AddErrorMessage("Refresh token has expired.");
            return output;
        }

        var user = await _userRepository.GetByIdAsync(currentRefreshToken.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] User {UserId} of refresh token {RefreshTokenId} not found.",
                input.CorrelationId,
                currentRefreshToken.UserId,
                currentRefreshToken.Id
            );

            output.AddErrorMessage("User not found.");
            return output;
        }

        var accessToken = _jwtService.GenerateToken(user);
        var generatedRefreshToken = _refreshTokenService.Generate();
        var newRefreshToken = generatedRefreshToken.MapToDomain(user.Id);

        currentRefreshToken.Revoke(newRefreshToken.Id);

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        _refreshTokenRepository.Update(currentRefreshToken);

        var saved = await _refreshTokenRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] Error rotating refresh token {RefreshTokenId}.",
                input.CorrelationId,
                currentRefreshToken.Id
            );

            output.AddErrorMessage("Error rotating refresh token.");
            return output;
        }

        output.AddResult(new LoginResponse(accessToken, generatedRefreshToken.Token));
        return output;
    }
}
