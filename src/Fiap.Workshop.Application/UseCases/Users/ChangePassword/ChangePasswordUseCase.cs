using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.ChangePassword.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.ChangePassword;

public sealed class ChangePasswordUseCase(
    IUserRepository userRepository,
    IPasswordService passwordService,
    ILogger<ChangePasswordUseCase> logger
) : IChangePasswordUseCase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly ILogger<ChangePasswordUseCase> _logger = logger;

    public async Task<Output> Handle(ChangePasswordInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var user = await _userRepository.GetByIdAsync(input.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Unable to find user by [{UserId}].",
                input.CorrelationId,
                input.UserId
            );

            output.AddErrorMessage("Unable to find user");
            return output;
        }

        if (!_passwordService.Verify(input.CurrentPassword, user.Password))
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Invalid current password for user [{UserId}].",
                input.CorrelationId,
                input.UserId
            );

            output.AddErrorMessage("Invalid current password");
            return output;
        }

        var newPasswordHash = _passwordService.Hash(input.NewPassword);
        user.ChangePassword(newPasswordHash);

        _userRepository.Update(user);

        var saved = await _userRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error changing password for user with id {UserId}.",
                input.CorrelationId,
                input.UserId
            );

            output.AddErrorMessage($"Error changing password for user with id {input.UserId}.");
            return output;
        }

        output.AddResult(user.MapToDto());
        return output;
    }
}
