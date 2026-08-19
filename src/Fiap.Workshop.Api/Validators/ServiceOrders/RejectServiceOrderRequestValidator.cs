using Fiap.Workshop.Api.Requests.ServiceOrders;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.ServiceOrders;

public sealed class RejectServiceOrderRequestValidator : AbstractValidator<RejectServiceOrderRequest>
{
    public RejectServiceOrderRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();
    }
}
