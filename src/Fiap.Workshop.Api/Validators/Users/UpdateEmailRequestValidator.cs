using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Domain;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Users;

public sealed class UpdateEmailRequestValidator : AbstractValidator<UpdateEmailRequest>
{
    public UpdateEmailRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .MaximumLength(User.EmailMaxLength)
            .EmailAddress();
    }
}
