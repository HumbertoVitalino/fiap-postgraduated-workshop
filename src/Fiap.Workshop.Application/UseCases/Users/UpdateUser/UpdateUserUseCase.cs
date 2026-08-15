using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Mapper;
using Fiap.Workshop.Application.UseCases.Users.UpdateUser.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.UpdateUser;

public sealed class UpdateUserUseCase(
    IUserRepository userRepository,
    ILogger<UpdateUserUseCase> logger
) : IUpdateUserUseCase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<UpdateUserUseCase> _logger = logger;

    public async Task<Output> Handle(UpdateUserInput input, CancellationToken cancellationToken)
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

        if (!string.Equals(user.Email, input.Email, StringComparison.Ordinal))
        {
            var emailInUse = await _userRepository.ExistsWithEmailAsync(input.Email, cancellationToken);
            if (emailInUse)
            {
                _logger.LogWarning(
                    "[{CorrelationId}] | User with email {Email} already exists.",
                    input.CorrelationId,
                    input.Email
                );

                output.AddErrorMessage($"User with email {input.Email} already exists.");
                return output;
            }
        }

        user.UpdateProfile(input.Name, input.Email, input.Role);

        _userRepository.Update(user);

        var saved = await _userRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error updating user with id {UserId}.",
                input.CorrelationId,
                input.UserId
            );

            output.AddErrorMessage($"Error updating user with id {input.UserId}.");
            return output;
        }

        output.AddResult(user.MapToDto());
        return output;
    }
}
