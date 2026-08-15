using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.GetUsers.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.GetUsers;

public class GetUsersUseCase(
    IUserRepository userRepository,
    ILogger<GetUsersUseCase> logger
) : IGetUsersUseCase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<GetUsersUseCase> _logger = logger;

    public async Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken)
    {
        Output output = new();

        var users = await _userRepository.GetAllAsync(cancellationToken);
        if (!users.Any())
        {
            _logger.LogWarning("[{CorrelationId}] | Unable to find users", correlationId);

            output.AddResult(Array.Empty<UserResponse>());
            return output;
        }

        output.AddResult(users.MapToDto());
        return output;
    }
}
