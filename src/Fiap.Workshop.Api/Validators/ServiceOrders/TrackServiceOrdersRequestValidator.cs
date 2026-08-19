using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.Commons;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.ServiceOrders;

public sealed class TrackServiceOrdersRequestValidator : AbstractValidator<TrackServiceOrdersRequest>
{
    private const int CPF_SIZE = 11;
    private const int CNPJ_SIZE = 14;

    public TrackServiceOrdersRequestValidator()
    {
        RuleFor(x => x.Document)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(document => document.Length is CPF_SIZE or CNPJ_SIZE)
            .WithMessage("Should be 11 or 14")
            .Must(document => document.IsValidDocument())
            .WithMessage("Invalid CPF/CNPJ.");

        RuleFor(x => x.LicensePlate)
            .NotEmpty()
            .Must(licensePlate => licensePlate.IsValidLicensePlate())
            .WithMessage("Invalid license plate. Must match the old (AAA0000) or Mercosul (AAA0A00) format.");
    }
}
