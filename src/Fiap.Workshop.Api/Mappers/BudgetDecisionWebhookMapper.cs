using Fiap.Workshop.Api.Requests.Webhooks;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class BudgetDecisionWebhookMapper
{
    public static ProcessBudgetDecisionInput MapToInput(this BudgetDecisionWebhookRequest request)
    {
        return new(
            request.CorrelationId,
            request.ServiceOrderId,
            request.Decision!.Value
        );
    }
}
