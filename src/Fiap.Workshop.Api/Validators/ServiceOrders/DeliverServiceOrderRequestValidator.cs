using Fiap.Workshop.Api.Requests.ServiceOrders;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.ServiceOrders;

public sealed class DeliverServiceOrderRequestValidator : AbstractValidator<DeliverServiceOrderRequest>
{
    public DeliverServiceOrderRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();
    }
}
