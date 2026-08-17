using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ApproveServiceOrder.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IApproveServiceOrderUseCase
{
    Task<Output> Handle(ApproveServiceOrderInput input, CancellationToken cancellationToken);
}
