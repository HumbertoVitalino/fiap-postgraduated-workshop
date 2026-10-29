using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Domain.Enums;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Users;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.CorrelationId)
            .NotEmpty();

        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(request => request.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(request => request.Role)
            .IsInEnum()
            .NotEqual(UserRole.System)
            .WithMessage("Role System is reserved for internal integrations.");
    }
}
