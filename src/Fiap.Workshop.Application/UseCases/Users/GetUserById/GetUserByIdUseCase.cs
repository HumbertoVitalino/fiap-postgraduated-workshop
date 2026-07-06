using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.GetUserById.Boundaries;
using Fiap.Workshop.Application.UseCases.Users.GetUserById.Mapper;
using Fiap.Workshop.Domain.Users;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.GetUserById;

public sealed class GetUserByIdUseCase(
    IUserRepository repository,
    ILogger<GetUserByIdUseCase> logger
) : IGetUserByIdUseCase
{
    private readonly IUserRepository _repository = repository;
    private readonly ILogger<GetUserByIdUseCase> _logger = logger;

    public async Task<Output> ExecuteAsync(GetUserByIdInput input, CancellationToken cancellationToken = default)
    {
        Output output = new();

        var user = await _repository.GetByIdAsync(input.Id, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "Get user failed: not found. UserId: {UserId} | CorrelationId: {CorrelationId}",
                input.Id, input.CorrelationId);
            output.AddErrorMessage(UserErrors.NotFound);
            return output;
        }

        output.AddResult(user.MapToOutput());
        return output;
    }
}
