using Fiap.Workshop.Api.Requests.Users;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Users;

public sealed class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(request => request.CorrelationId)
            .NotEmpty();

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(8);

        RuleFor(request => request.PasswordConfirmation)
            .Equal(request => request.Password)
            .WithMessage("PasswordConfirmation must match Password.");

        RuleFor(request => request.Role)
            .IsInEnum();
    }
}
