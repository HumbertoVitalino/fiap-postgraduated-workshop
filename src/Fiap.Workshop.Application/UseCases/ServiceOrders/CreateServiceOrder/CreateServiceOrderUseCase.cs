using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder;

public sealed class CreateServiceOrderUseCase(
    ICustomerRepository customerRepository,
    IVehicleRepository vehicleRepository,
    IServiceOrderRepository serviceOrderRepository,
    ILogger<CreateServiceOrderUseCase> logger
) : ICreateServiceOrderUseCase
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IVehicleRepository _vehicleRepository = vehicleRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<CreateServiceOrderUseCase> _logger = logger;

    public async Task<Output> Handle(CreateServiceOrderInput input, CancellationToken cancellationToken)
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

        if (vehicle.CustomerId != input.CustomerId)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Vehicle [{VehicleId}] does not belong to customer [{CustomerId}].",
                input.CorrelationId,
                input.VehicleId,
                input.CustomerId
            );

            output.AddErrorMessage("Vehicle does not belong to the specified customer");
            return output;
        }

        var serviceOrder = input.MapToDomain();

        await _serviceOrderRepository.AddAsync(serviceOrder, cancellationToken);

        var saved = await _serviceOrderRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error saving service order for vehicle [{VehicleId}].",
                input.CorrelationId,
                input.VehicleId
            );

            output.AddErrorMessage("Error saving service order");
            return output;
        }

        output.AddResult(serviceOrder.MapToDto());
        return output;
    }
}
