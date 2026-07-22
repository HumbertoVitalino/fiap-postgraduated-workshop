using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.UpdateEmail.Boundaries;
using Fiap.Workshop.Domain.Users;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.UpdateEmail;

public sealed class UpdateEmailUseCase(
    IUserRepository repository,
    ILogger<UpdateEmailUseCase> logger
) : IUpdateEmailUseCase
{
    private readonly IUserRepository _repository = repository;
    private readonly ILogger<UpdateEmailUseCase> _logger = logger;

    public async Task<Output> ExecuteAsync(UpdateEmailInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var user = await _repository.GetByIdAsync(input.UserId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Update email failed: user not found. UserId: {UserId}",
                input.CorrelationId,
                input.UserId
            );

            output.AddErrorMessage(UserErrors.NotFound);
            return output;
        }

        var email = input.Email.Trim().ToLowerInvariant();
        if (email == user.Email)
        {
            output.AddResult(UserResponse.FromUser(user));
            return output;
        }

        if (await _repository.ExistsWithEmailAsync(email, cancellationToken))
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Update email failed: email already in use. UserId: {UserId}",
                input.CorrelationId,
                input.UserId
            );

            output.AddErrorMessage(UserErrors.EmailAlreadyInUse);
            return output;
        }

        user.UpdateEmail(email);
        _repository.Update(user);

        var isSaved = await _repository.UnitOfWork.CommitAsync(cancellationToken);
        if (!isSaved)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Update email failed: could not persist. UserId: {UserId}",
                input.CorrelationId,
                input.UserId
            );

            output.AddErrorMessage("Failed to persist user.");
            return output;
        }

        output.AddResult(UserResponse.FromUser(user));
        return output;
    }
}
