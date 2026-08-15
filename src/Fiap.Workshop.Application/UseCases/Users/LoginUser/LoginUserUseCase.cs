using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Users.LoginUser.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Users.LoginUser;

public class LoginUserUseCase(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IJwtService jwtService,
    ILogger<LoginUserUseCase> logger
) : ILoginUserUseCase
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly IPasswordService _passwordService = passwordService;
    private readonly IJwtService _jwtService = jwtService;
    private readonly ILogger<LoginUserUseCase> _logger = logger;

    public async Task<Output> Handle(LoginUserInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var user = await _userRepository.GetByEmailAsync(input.Email, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] User not found with email: {Email}",
                input.CorrelationId,
                input.Email
            );

            output.AddErrorMessage("User not found.");
            return output;
        }

        var isPasswordValid = _passwordService.Verify(input.Password, user.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning(
                "[{CorrelationId}] Invalid password for user: {Email}",
                input.CorrelationId,
                input.Email
            );

            output.AddErrorMessage("Invalid password.");
            return output;
        }

        var token = _jwtService.GenerateToken(user);

        output.AddResult(token);
        return output;
    }
}
