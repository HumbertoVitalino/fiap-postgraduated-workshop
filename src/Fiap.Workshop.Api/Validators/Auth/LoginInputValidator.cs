using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Domain.Users;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(Email.MaxLength)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
