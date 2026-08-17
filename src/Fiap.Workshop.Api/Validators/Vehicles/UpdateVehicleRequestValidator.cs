using Fiap.Workshop.Api.Requests.Vehicles;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Vehicles;

public sealed class UpdateVehicleRequestValidator : AbstractValidator<UpdateVehicleRequest>
{
    public UpdateVehicleRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.Brand)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Model)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Color)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.ModelYear)
            .GreaterThan(0);
    }
}
