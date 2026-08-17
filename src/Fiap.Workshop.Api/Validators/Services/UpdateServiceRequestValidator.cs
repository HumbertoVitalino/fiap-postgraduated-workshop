using Fiap.Workshop.Api.Requests.Services;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Services;

public sealed class UpdateServiceRequestValidator : AbstractValidator<UpdateServiceRequest>
{
    public UpdateServiceRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.BasePrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.EstimatedDuration)
            .GreaterThanOrEqualTo((short)0);
    }
}
