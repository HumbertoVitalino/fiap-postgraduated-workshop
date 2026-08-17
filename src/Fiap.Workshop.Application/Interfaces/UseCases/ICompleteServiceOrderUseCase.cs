using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ICompleteServiceOrderUseCase
{
    Task<Output> Handle(CompleteServiceOrderInput input, CancellationToken cancellationToken);
}
