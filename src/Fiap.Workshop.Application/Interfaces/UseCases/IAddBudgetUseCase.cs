using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.AddBudget.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IAddBudgetUseCase
{
    Task<Output> Handle(AddBudgetInput input, CancellationToken cancellationToken);
}
