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

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("The password is required.")
            .MinimumLength(10).WithMessage("The password must be at least 10 characters long.")
            .Matches("[A-Z]").WithMessage("The password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("The password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("The password must contain at least one number.");

        RuleFor(x => x.PasswordConfirmation)
            .Equal(x => x.Password).WithMessage("The password and confirmation do not match.");
    }
}
