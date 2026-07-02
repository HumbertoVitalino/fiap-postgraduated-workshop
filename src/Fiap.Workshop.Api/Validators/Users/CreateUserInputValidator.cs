using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Domain.Users;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(Email.MaxLength)
            .EmailAddress();
    }
}
