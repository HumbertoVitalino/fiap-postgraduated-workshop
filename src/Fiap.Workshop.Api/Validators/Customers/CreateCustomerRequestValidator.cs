using Fiap.Workshop.Api.Requests.Customers;
using Fiap.Workshop.Application.Commons;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Customers;

public sealed class CreateCustomerRequestValidator : AbstractValidator<CreateCustomerRequest>
{
    private const int CPF_SIZE = 11;
    private const int CNPJ_SIZE = 14;

    public CreateCustomerRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Document)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(document => document.Length is CPF_SIZE or CNPJ_SIZE)
            .WithMessage("Should be 11 or 14")
            .Must(document => document.IsValidDocument())
            .WithMessage("Invalid CPF/CNPJ.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}
