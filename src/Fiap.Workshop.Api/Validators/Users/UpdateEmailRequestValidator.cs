using Fiap.Workshop.Api.Requests.Users;
using Fiap.Workshop.Domain.Users;
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
