using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Mapper;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.Services.CreateService;

public sealed class CreateServiceUseCase(
    IServiceRepository serviceRepository,
    ILogger<CreateServiceUseCase> logger
) : ICreateServiceUseCase
{
    private readonly IServiceRepository _serviceRepository = serviceRepository;
    private readonly ILogger<CreateServiceUseCase> _logger = logger;

    public async Task<Output> Handle(CreateServiceInput input, CancellationToken cancellationToken)
    {
        Output output = new();

        var codeInUse = await _serviceRepository.ExistsWithCodeAsync(input.Code, cancellationToken);
        if (codeInUse)
        {
            _logger.LogWarning(
                "[{CorrelationId}] | Service with code {Code} already exists.",
                input.CorrelationId,
                input.Code
            );

            output.AddErrorMessage($"Service with code {input.Code} already exists.");
            return output;
        }

        var service = input.MapToDomain();

        await _serviceRepository.AddAsync(service, cancellationToken);

        var saved = await _serviceRepository.UnitOfWork.CommitAsync(cancellationToken);
        if (!saved)
        {
            _logger.LogError(
                "[{CorrelationId}] | Error saving service with code {Code}.",
                input.CorrelationId,
                input.Code
            );

            output.AddErrorMessage($"Error saving service with code {input.Code}.");
            return output;
        }

        output.AddResult(service.MapToDto());
        return output;
    }
}
