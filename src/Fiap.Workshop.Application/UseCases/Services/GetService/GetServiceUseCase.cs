using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Mapper;
using Fiap.Workshop.Application.UseCases.Services.GetService.Boundaries;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Services.GetService;

public sealed class GetServiceUseCase(
    IServiceRepository serviceRepository,
    ILogger<GetServiceUseCase> logger
) : IGetServiceUseCase
{
    private readonly IServiceRepository _serviceRepository = serviceRepository;
    private readonly ILogger<GetServiceUseCase> _logger = logger;

    public async Task<Output> Handle(GetServiceInput input, CancellationToken cancellationToken)
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

        output.AddResult(service.MapToDto());

        return output;
    }
}
