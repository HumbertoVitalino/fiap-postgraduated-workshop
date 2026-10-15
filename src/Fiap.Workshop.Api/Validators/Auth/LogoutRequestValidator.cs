using Fiap.Workshop.Api.Requests.Auth;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Auth;

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(request => request.CorrelationId)
            .NotEmpty();

        RuleFor(request => request.RefreshToken)
            .NotEmpty();
    }
}
