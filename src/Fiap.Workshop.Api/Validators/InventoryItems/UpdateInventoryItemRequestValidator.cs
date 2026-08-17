using Fiap.Workshop.Api.Requests.InventoryItems;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.InventoryItems;

public sealed class UpdateInventoryItemRequestValidator : AbstractValidator<UpdateInventoryItemRequest>
{
    public UpdateInventoryItemRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(1000);

        RuleFor(x => x.UnitPrice)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.MinimumStock)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.UnitOfMeasure)
            .IsInEnum();
    }
}
