using Fiap.Workshop.Api.Requests.ServiceOrders;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.ServiceOrders;

public sealed class StartDiagnosisRequestValidator : AbstractValidator<StartDiagnosisRequest>
{
    public StartDiagnosisRequestValidator()
    {
        RuleFor(x => x.CorrelationId)
            .NotEmpty();

        RuleFor(x => x.DiagnoseDescription)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
