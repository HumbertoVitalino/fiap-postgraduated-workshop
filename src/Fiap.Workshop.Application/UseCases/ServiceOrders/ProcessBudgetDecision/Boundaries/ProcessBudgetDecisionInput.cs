namespace Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision.Boundaries;

public sealed record ProcessBudgetDecisionInput(
    Guid CorrelationId,
    Guid ServiceOrderId,
    BudgetDecision Decision
);
