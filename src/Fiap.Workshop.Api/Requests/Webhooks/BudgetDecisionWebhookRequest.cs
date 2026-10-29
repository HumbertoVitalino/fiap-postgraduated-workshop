using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision.Boundaries;

namespace Fiap.Workshop.Api.Requests.Webhooks;

public sealed record BudgetDecisionWebhookRequest(
    Guid CorrelationId,
    Guid ServiceOrderId,
    BudgetDecision? Decision
);
