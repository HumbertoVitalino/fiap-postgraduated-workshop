using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetServiceOrderStatusUseCase
{
    Task<Output> Handle(GetServiceOrderStatusInput input, CancellationToken cancellationToken);
}
