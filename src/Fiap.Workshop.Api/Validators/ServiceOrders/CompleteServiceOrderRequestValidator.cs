using Fiap.Workshop.Api.Requests.ServiceOrders;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.ServiceOrders;

public sealed class CompleteServiceOrderRequestValidator : AbstractValidator<CompleteServiceOrderRequest>
{
    public CompleteServiceOrderRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.ServiceDurations)
            .NotNull();

        RuleForEach(x => x.ServiceDurations).ChildRules(duration =>
        {
            duration.RuleFor(x => x.ServiceOrderServiceId)
                .NotEmpty();

            duration.RuleFor(x => x.ActualDuration)
                .GreaterThan((short)0);
        });
    }
}
