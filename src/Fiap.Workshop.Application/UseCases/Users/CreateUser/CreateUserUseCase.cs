using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Mapper;
using Fiap.Workshop.Domain.Users;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.CreateUser;

public sealed class CreateUserUseCase(
    IUserRepository repository,
    IPasswordHasher passwordHasher,
    ILogger<CreateUserUseCase> logger
) : ICreateUserUseCase
{
    private readonly IUserRepository _userRepository = repository;
    private readonly IPasswordHasher _passwordHasher = passwordHasher;
    private readonly ILogger<CreateUserUseCase> _logger = logger;

    public async Task<Output> ExecuteAsync(CreateUserInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var email = input.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsWithEmailAsync(email, cancellationToken))
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Create user failed: email already in use. Email: {Email}",
                input.CorrelationId,
                input.Email
            );

            output.AddErrorMessage(UserErrors.EmailAlreadyInUse);
            return output;
        }

        var user = input.MapToDomain(_passwordHasher);

        await _userRepository.AddAsync(user, cancellationToken);

        var isSaved = await _userRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!isSaved)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Create user failed: could not persist. Email: {Email}",
                input.CorrelationId,
                input.Email
            );

            output.AddErrorMessage("Failed to persist user.");
            return output;
        }

        output.AddResult(user.MapToOutput());
        return output;
    }
}
