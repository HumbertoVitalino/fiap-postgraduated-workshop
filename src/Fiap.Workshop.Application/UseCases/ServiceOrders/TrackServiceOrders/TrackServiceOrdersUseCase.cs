using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders;

public sealed class TrackServiceOrdersUseCase(
    IVehicleRepository vehicleRepository,
    ICustomerRepository customerRepository,
    IServiceOrderRepository serviceOrderRepository,
    ILogger<TrackServiceOrdersUseCase> logger
) : ITrackServiceOrdersUseCase
{
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<TrackServiceOrdersUseCase> _logger = logger;

    public async Task<Output> Handle(TrackServiceOrdersInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var vehicle = await _vehicleRepository.GetByLicensePlateAsync(input.LicensePlate, cancellationToken);
        if (vehicle is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Unable to find vehicle by license plate for tracking lookup.",
                input.CorrelationId
            );

            output.AddResult(Array.Empty<ServiceOrderTrackingResponse>());
            return output;
        }

        var customer = await _customerRepository.GetByIdAsync(vehicle.CustomerId, cancellationToken);
        if (customer is null || customer.Document != input.Document)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Document mismatch or missing customer for tracking lookup.",
                input.CorrelationId
            );

            output.AddResult(Array.Empty<ServiceOrderTrackingResponse>());
            return output;
        }

        var serviceOrders = await _serviceOrderRepository.GetAllByVehicleIdAsync(vehicle.Id, cancellationToken);

        output.AddResult(serviceOrders.MapToDto());
        return output;
    }
}
