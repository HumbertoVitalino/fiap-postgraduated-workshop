using Fiap.Workshop.Api.Requests.Auth;
using Fiap.Workshop.Domain;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(User.EmailMaxLength)
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty();
    }
}
