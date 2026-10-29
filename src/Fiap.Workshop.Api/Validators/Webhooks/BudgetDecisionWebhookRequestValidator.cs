using Fiap.Workshop.Api.Requests.Webhooks;
using FluentValidation;

namespace Fiap.Workshop.Api.Validators.Webhooks;

public sealed class BudgetDecisionWebhookRequestValidator : AbstractValidator<BudgetDecisionWebhookRequest>
{
    public BudgetDecisionWebhookRequestValidator()
    {
        RuleFor(request => request.CorrelationId)
            .NotEmpty();

        RuleFor(request => request.ServiceOrderId)
            .NotEmpty();

        RuleFor(request => request.Decision)
            .NotNull()
            .IsInEnum();
    }
}
