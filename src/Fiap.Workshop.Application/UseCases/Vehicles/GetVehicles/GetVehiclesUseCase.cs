using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Vehicle;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Vehicles.GetVehicles.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Vehicles.GetVehicles;

public sealed class GetVehiclesUseCase(
    IVehicleRepository vehicleRepository,
    ILogger<GetVehiclesUseCase> logger
) : IGetVehiclesUseCase
{
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly ILogger<GetVehiclesUseCase> _logger = logger;

    public async Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken)
    {
        Output output = new();

        var vehicles = await _vehicleRepository.GetAllAsync(cancellationToken);
        if (!vehicles.Any())
        {
            _logger.LogWarning("[{CorrelationId}] | Unable to find vehicles", correlationId);

            output.AddResult(Array.Empty<VehicleResponse>());
            return output;
        }

        output.AddResult(vehicles.MapToDto());
        return output;
    }
}
