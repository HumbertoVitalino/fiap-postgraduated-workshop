using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IProcessBudgetDecisionUseCase
{
    Task<Output> Handle(ProcessBudgetDecisionInput input, CancellationToken cancellationToken);
}
