using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IUpdateVehicleUseCase
{
    Task<Output> Handle(UpdateVehicleInput input, CancellationToken cancellationToken);
}
