using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Vehicles.CreateVehicle.Mapper;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Vehicles.GetVehicle;

public sealed class GetVehicleUseCase(
    IVehicleRepository vehicleRepository,
    ILogger<GetVehicleUseCase> logger
) : IGetVehicleUseCase
{
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly ILogger<GetVehicleUseCase> _logger = logger;

    public async Task<Output> Handle(GetVehicleInput input, CancellationToken cancellationToken)
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

        output.AddResult(vehicle.MapToDto());

        return output;
    }
}
