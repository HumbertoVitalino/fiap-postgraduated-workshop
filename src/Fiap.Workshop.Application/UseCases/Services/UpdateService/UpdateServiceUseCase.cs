using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Mapper;
using Fiap.Workshop.Application.UseCases.Services.UpdateService.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Services.UpdateService;

public sealed class UpdateServiceUseCase(
    IServiceRepository serviceRepository,
    ILogger<UpdateServiceUseCase> logger
) : IUpdateServiceUseCase
{
    private readonly IServiceRepository _serviceRepository = serviceRepository;
    private readonly ILogger<UpdateServiceUseCase> _logger = logger;

    public async Task<Output> Handle(UpdateServiceInput input, CancellationToken cancellationToken)
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

            output.AddErrorMessage("Unable to find service");
            return output;
        }

        service.UpdateProfile(input.Name, input.Description, input.BasePrice, input.EstimatedDuration, input.IsActive);

        _serviceRepository.Update(service);

        var saved = await _serviceRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error updating service with id {ServiceId}.",
                input.CorrelationId,
                input.ServiceId
            );

            output.AddErrorMessage($"Error updating service with id {input.ServiceId}.");
            return output;
        }

        output.AddResult(service.MapToDto());
        return output;
    }
}
