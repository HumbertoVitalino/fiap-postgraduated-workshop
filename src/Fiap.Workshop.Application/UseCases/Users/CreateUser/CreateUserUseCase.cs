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
    ILogger<CreateUserUseCase> logger
) : ICreateUserUseCase
{
    private readonly IUserRepository _repository = repository;
    private readonly ILogger<CreateUserUseCase> _logger = logger;

    public async Task<Output> ExecuteAsync(CreateUserInput input, CancellationToken cancellationToken = default)
    {
        Output output = new();

        var email = Email.Create(input.Email);

        if (await _repository.ExistsWithEmailAsync(email, cancellationToken))
        {
            _logger.LogWarning(
                "Create user failed: email already in use. Email: {Email} | CorrelationId: {CorrelationId}",
                input.Email, input.CorrelationId
            );

            output.AddErrorMessage(UserErrors.EmailAlreadyInUse);
            return output;
        }

        var user = User.Create(input.Email, input.Name, input.Role);

        await _repository.AddAsync(user, cancellationToken);

        var isSaved = await _repository.UnitOfWork.CommitAsync(cancellationToken);
        if (!isSaved)
        {
            _logger.LogWarning(
                "Create user failed: could not persist. Email: {Email} | CorrelationId: {CorrelationId}",
                input.Email, input.CorrelationId);
            output.AddErrorMessage("Failed to persist user.");
            return output;
        }

        output.AddResult(user.MapToOutput());
        return output;
    }
}
