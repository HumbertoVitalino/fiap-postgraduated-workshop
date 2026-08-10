using Fiap.Workshop.Api.Requests.Auth;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.CorrelationId)
            .NotEmpty();

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8);
    }
}
