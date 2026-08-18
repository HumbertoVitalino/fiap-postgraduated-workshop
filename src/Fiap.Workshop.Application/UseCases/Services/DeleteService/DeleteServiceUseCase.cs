using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.DeleteService.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Services.DeleteService;

public sealed class DeleteServiceUseCase(
    IServiceRepository serviceRepository,
    IServiceOrderRepository serviceOrderRepository,
    ILogger<DeleteServiceUseCase> logger
) : IDeleteServiceUseCase
{
    private readonly IServiceRepository _serviceRepository = serviceRepository;
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<DeleteServiceUseCase> _logger = logger;

    public async Task<Output> Handle(DeleteServiceInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var service = await _serviceRepository.GetByIdAsync(input.ServiceId, cancellationToken);
        if (service is null)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Unable to find service by [{ServiceId}].",
                input.CorrelationId,
                input.ServiceId
            );

            return output;
        }

        var hasBeenUsed = await _serviceOrderRepository.ExistsWithServiceIdAsync(service.Id, cancellationToken);
        if (hasBeenUsed)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Service with id {ServiceId} has been used in a service order and cannot be deleted.",
                input.CorrelationId,
                input.ServiceId
            );

            output.AddErrorMessage("Service has been used in a service order and cannot be deleted.");
            return output;
        }

        _serviceRepository.Remove(service);

        var saved = await _serviceRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error deleting service with id {ServiceId}.",
                input.CorrelationId,
                input.ServiceId
            );

            output.AddErrorMessage($"Error deleting service with id {input.ServiceId}.");
            return output;
        }

        return output;
    }
}
