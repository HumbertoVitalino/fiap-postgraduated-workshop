using Fiap.Workshop.Api.Requests.ServiceOrders;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.ServiceOrders;

public sealed class AddBudgetRequestValidator : AbstractValidator<AddBudgetRequest>
{
    public AddBudgetRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.Services)
            .NotNull();

        RuleFor(x => x.Parts)
            .NotNull();

        RuleForEach(x => x.Services).ChildRules(service =>
        {
            service.RuleFor(x => x.ServiceId)
                .NotEmpty();

            service.RuleFor(x => x.Quantity)
                .GreaterThan(0);
        });

        RuleForEach(x => x.Parts).ChildRules(part =>
        {
            part.RuleFor(x => x.InventoryItemId)
                .NotEmpty();

            part.RuleFor(x => x.Quantity)
                .GreaterThan(0);
        });
    }
}
