using Fiap.Workshop.Api.Requests.ServiceOrders;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.ServiceOrders;

public sealed class ApproveServiceOrderRequestValidator : AbstractValidator<ApproveServiceOrderRequest>
{
    public ApproveServiceOrderRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();
    }
}
