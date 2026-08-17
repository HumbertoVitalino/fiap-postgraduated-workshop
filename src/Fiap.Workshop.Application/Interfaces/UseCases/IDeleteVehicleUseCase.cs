using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IDeleteVehicleUseCase
{
    Task<Output> Handle(DeleteVehicleInput input, CancellationToken cancellationToken);
}
