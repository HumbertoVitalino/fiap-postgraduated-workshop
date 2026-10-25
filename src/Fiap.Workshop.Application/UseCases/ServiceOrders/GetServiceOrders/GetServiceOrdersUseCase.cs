using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders.Mapper;
using Fiap.Workshop.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders;

public sealed class GetServiceOrdersUseCase(
    IServiceOrderRepository serviceOrderRepository,
    ILogger<GetServiceOrdersUseCase> logger
) : IGetServiceOrdersUseCase
{
    private readonly IServiceOrderRepository _serviceOrderRepository = serviceOrderRepository;
    private readonly ILogger<GetServiceOrdersUseCase> _logger = logger;

    private static readonly ServiceOrderStatus[] ListingPriority =
    [
        ServiceOrderStatus.InProgress,
        ServiceOrderStatus.AwaitingApproval,
        ServiceOrderStatus.Diagnosing,
        ServiceOrderStatus.Received,
        ServiceOrderStatus.Cancelled
    ];

    public async Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken)
    {
        Output output = new();

        var serviceOrders = await _serviceOrderRepository.GetByStatusesAsync(ListingPriority, cancellationToken);
        if (serviceOrders.Count == 0)
        {
            _logger.LogWarning("[{CorrelationId}] | Unable to find service orders", correlationId);

            output.AddResult(Array.Empty<ServiceOrderResponse>());
            return output;
        }

        var ordered = serviceOrders
            .OrderBy(so => Array.IndexOf(ListingPriority, so.Status))
            .ThenBy(so => so.OpenedAt);

        output.AddResult(ordered.MapToDto());
        return output;
    }
}
