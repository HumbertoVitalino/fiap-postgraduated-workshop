using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.CreateUser.Boundaries;
using Fiap.Workshop.Application.UseCases.CreateUser.Mapper;
using Fiap.Workshop.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.CreateUser;

public sealed class CreateUserUseCase(
    IUserRepository userRepository,
    IPasswordService passwordService,
    ILogger<CreateUserUseCase> logger
) : ICreateUserUseCase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly ILogger<CreateUserUseCase> _logger = logger;

    public async Task<Output> Handle(CreateUserInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var isAnyUser = await _userRepository.ExistsWithEmailAsync(input.Email, cancellationToken);
        if (isAnyUser)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | User with email {Email} already exists.",
                input.CorrelationId,
                input.Email
            );

            output.AddErrorMessage($"User with email {input.Email} already exists.");

            return output;
        }

        var passwordHash = _passwordService.Hash(input.Password);
        var user = input.MapToDomain(passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);

        var saved = await _userRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error saving user with email {Email}.",
                input.CorrelationId,
                input.Email
            );

            output.AddErrorMessage($"Error saving user with email {input.Email}.");
            return output;
        }

        output.AddResult(user);
        return output;
    }
}
