using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Users;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.Login.Boundaries;
using Fiap.Workshop.Domain.Users;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.Login;

public sealed class LoginUseCase(
    IUserRepository userRepository,
    IJwtService jwtService,
    ILogger<LoginUseCase> logger
) : ILoginUseCase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IJwtService _jwtService = jwtService;
    private readonly ILogger<LoginUseCase> _logger = logger;

    public async Task<Output> ExecuteAsync(LoginInput input, CancellationToken cancellationToken = default)
    {
        Output output = new();

        var email = Email.Create(input.Email);
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "Login failed: user not found. Email: {Email} | CorrelationId: {CorrelationId}",
                input.Email, input.CorrelationId);
            output.AddErrorMessage("Invalid credentials.");
            return output;
        }

        var token = _jwtService.GenerateToken(user.Id.ToString(), user.Email.Value, user.Role.ToString());
        output.AddResult(new LoginResponse(token));
        return output;
    }
}
