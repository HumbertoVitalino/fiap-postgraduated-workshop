using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetVehicleUseCase
{
    Task<Output> Handle(GetVehicleInput input, CancellationToken cancellationToken);
}
