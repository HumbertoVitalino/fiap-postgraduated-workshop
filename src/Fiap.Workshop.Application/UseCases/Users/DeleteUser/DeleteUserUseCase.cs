using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.DeleteUser.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.DeleteUser;

public sealed class DeleteUserUseCase(
    IUserRepository userRepository,
    ILogger<DeleteUserUseCase> logger
) : IDeleteUserUseCase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<DeleteUserUseCase> _logger = logger;

    public async Task<Output> Handle(DeleteUserInput input, CancellationToken cancellationToken)
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

            return output;
        }

        _userRepository.Remove(user);

        var saved = await _userRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error deleting user with id {UserId}.",
                input.CorrelationId,
                input.UserId
            );

            output.AddErrorMessage($"Error deleting user with id {input.UserId}.");
            return output;
        }

        return output;
    }
}
