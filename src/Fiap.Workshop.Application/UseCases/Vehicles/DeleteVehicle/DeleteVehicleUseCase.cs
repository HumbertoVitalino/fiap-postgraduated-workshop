using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Vehicles.DeleteVehicle;

public sealed class DeleteVehicleUseCase(
    IVehicleRepository vehicleRepository,
    IServiceOrderRepository serviceOrderRepository,
    ILogger<DeleteVehicleUseCase> logger
) : IDeleteVehicleUseCase
{
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<DeleteVehicleUseCase> _logger = logger;

    public async Task<Output> Handle(DeleteVehicleInput input, CancellationToken cancellationToken)
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

            return output;
        }

        var hasServiceOrders = await _serviceOrderRepository.ExistsWithVehicleIdAsync(vehicle.Id, cancellationToken);
        if (hasServiceOrders)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Vehicle with id {VehicleId} has service orders associated and cannot be deleted.",
                input.CorrelationId,
                input.VehicleId
            );

            output.AddErrorMessage("Vehicle has service orders associated and cannot be deleted.");
            return output;
        }

        _vehicleRepository.Remove(vehicle);

        var saved = await _vehicleRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error deleting vehicle with id {VehicleId}.",
                input.CorrelationId,
                input.VehicleId
            );

            output.AddErrorMessage($"Error deleting vehicle with id {input.VehicleId}.");
            return output;
        }

        return output;
    }
}
