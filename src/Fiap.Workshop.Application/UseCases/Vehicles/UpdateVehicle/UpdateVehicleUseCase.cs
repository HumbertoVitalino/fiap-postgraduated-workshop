using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Mapper;
using Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Vehicles.UpdateVehicle;

public sealed class UpdateVehicleUseCase(
    IVehicleRepository vehicleRepository,
    ILogger<UpdateVehicleUseCase> logger
) : IUpdateVehicleUseCase
{
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly ILogger<UpdateVehicleUseCase> _logger = logger;

    public async Task<Output> Handle(UpdateVehicleInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var vehicle = await _vehicleRepository.GetByIdAsync(input.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Unable to find vehicle by [{VehicleId}].",
                input.CorrelationId,
                input.VehicleId
            );

            output.AddErrorMessage("Unable to find vehicle");
            return output;
        }

        vehicle.UpdateProfile(input.Brand, input.Model, input.Color, input.ModelYear);

        _vehicleRepository.Update(vehicle);

        var saved = await _vehicleRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error updating vehicle with id {VehicleId}.",
                input.CorrelationId,
                input.VehicleId
            );

            output.AddErrorMessage($"Error updating vehicle with id {input.VehicleId}.");
            return output;
        }

        output.AddResult(vehicle.MapToDto());
        return output;
    }
}
