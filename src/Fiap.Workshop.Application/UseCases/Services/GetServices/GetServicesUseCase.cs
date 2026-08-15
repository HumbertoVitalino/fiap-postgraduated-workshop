using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.GetServices.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Services.GetServices;

public sealed class GetServicesUseCase(
    IServiceRepository serviceRepository,
    ILogger<GetServicesUseCase> logger
) : IGetServicesUseCase
{
    private readonly IServiceRepository _serviceRepository = serviceRepository;
    private readonly ILogger<GetServicesUseCase> _logger = logger;

    public async Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken)
    {
        Output output = new();

        var services = await _serviceRepository.GetAllAsync(cancellationToken);
        if (!services.Any())
        {
            _logger.LogWarning("[{CorrelationId}] | Unable to find services", correlationId);

            output.AddResult(Array.Empty<ServiceResponse>());
            return output;
        }

        output.AddResult(services.MapToDto());
        return output;
    }
}
