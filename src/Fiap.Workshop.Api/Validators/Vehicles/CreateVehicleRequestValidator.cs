using Fiap.Workshop.Api.Requests.Vehicles;
using Fiap.Workshop.Application.Commons;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Vehicles;

public sealed class CreateVehicleRequestValidator : AbstractValidator<CreateVehicleRequest>
{
    public CreateVehicleRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.CustomerId)
            .NotEmpty();

        RuleFor(x => x.LicensePlate)
            .NotEmpty()
            .Must(licensePlate => licensePlate.IsValidLicensePlate())
            .WithMessage("Invalid license plate. Must match the old (AAA0000) or Mercosul (AAA0A00) format.");

        RuleFor(x => x.Brand)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Model)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Color)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.ManufactureYear)
            .GreaterThan(0);

        RuleFor(x => x.ModelYear)
            .GreaterThanOrEqualTo(x => x.ManufactureYear)
            .WithMessage("Model year cannot be earlier than manufacture year.");
    }
}
