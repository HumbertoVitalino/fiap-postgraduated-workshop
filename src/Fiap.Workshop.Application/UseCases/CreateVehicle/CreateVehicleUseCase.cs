using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.CreateVehicle.Boundaries;
using Fiap.Workshop.Application.UseCases.CreateVehicle.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.CreateVehicle;

public sealed class CreateVehicleUseCase(
    ICustomerRepository customerRepository,
    IVehicleRepository vehicleRepository,
    ILogger<CreateVehicleUseCase> logger
) : ICreateVehicleUseCase
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly ILogger<CreateVehicleUseCase> _logger = logger;

    public async Task<Output> Handle(CreateVehicleInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var customer = await _customerRepository.GetByIdAsync(input.CustomerId, cancellationToken);
        if (customer is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Unable to find customer by [{CustomerId}].",
                input.CorrelationId,
                input.CustomerId
            );

            output.AddErrorMessage("Unable to find customer");
            return output;
        }

        var plateInUse = await _vehicleRepository.ExistsWithLicensePlateAsync(input.LicensePlate, cancellationToken);
        if (plateInUse)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Vehicle with license plate {LicensePlate} already exists.",
                input.CorrelationId,
                input.LicensePlate
            );

            output.AddErrorMessage($"Vehicle with license plate {input.LicensePlate} already exists.");
            return output;
        }

        var vehicle = input.MapToDomain();

        await _vehicleRepository.AddAsync(vehicle, cancellationToken);

        var saved = await _vehicleRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error saving vehicle with license plate {LicensePlate}.",
                input.CorrelationId,
                input.LicensePlate
            );

            output.AddErrorMessage($"Error saving vehicle with license plate {input.LicensePlate}.");
            return output;
        }

        output.AddResult(vehicle.MapToDto());
        return output;
    }
}
