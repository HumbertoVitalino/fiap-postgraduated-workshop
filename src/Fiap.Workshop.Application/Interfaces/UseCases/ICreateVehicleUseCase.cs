using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ICreateVehicleUseCase
{
    Task<Output> Handle(CreateVehicleInput input, CancellationToken cancellationToken);
}
