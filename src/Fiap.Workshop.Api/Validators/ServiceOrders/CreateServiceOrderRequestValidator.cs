using Fiap.Workshop.Api.Requests.ServiceOrders;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.ServiceOrders;

public sealed class CreateServiceOrderRequestValidator : AbstractValidator<CreateServiceOrderRequest>
{
    public CreateServiceOrderRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.CustomerId)
            .NotEmpty();

        RuleFor(x => x.VehicleId)
            .NotEmpty();

        RuleFor(x => x.ProblemDescription)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.OdometerReading)
            .GreaterThanOrEqualTo(0);
    }
}
