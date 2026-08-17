using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ITrackServiceOrdersUseCase
{
    Task<Output> Handle(TrackServiceOrdersInput input, CancellationToken cancellationToken);
}
