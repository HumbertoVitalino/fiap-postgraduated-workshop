using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Fiap.Workshop.Application.EventHandlers.ServiceOrders;

public sealed class SendStatusChangedEmailHandler(
    ICustomerRepository customerRepository,
    IEmailService emailService,
    ILogger<SendStatusChangedEmailHandler> logger
) : IDomainEventHandler<ServiceOrderStatusChangedEvent>
{
    private readonly ICustomerRepository _customerRepository = customerRepository;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<SendStatusChangedEmailHandler> _logger = logger;

    public async Task HandleAsync(ServiceOrderStatusChangedEvent domainEvent, CancellationToken cancellationToken)
    {
        var customer = await _customerRepository.GetByIdAsync(domainEvent.CustomerId, cancellationToken);
        if (customer is null)
        {
            _logger.LogWarning(
                "Unable to find customer [{CustomerId}] to notify status change of service order [{ServiceOrderId}].",
                domainEvent.CustomerId,
                domainEvent.ServiceOrderId
            );

            return;
        }

        var currentStatus = ToDisplayName(domainEvent.CurrentStatus);
        var message = new EmailMessage(
            customer.Email,
            $"Sua ordem de serviço agora está \"{currentStatus}\"",
            $"Olá, {customer.Name}.\n\n" +
            $"O status da sua ordem de serviço mudou de \"{ToDisplayName(domainEvent.PreviousStatus)}\" " +
            $"para \"{currentStatus}\" em {domainEvent.ChangedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture)}.\n\n" +
            "Esta é uma mensagem automática, não é necessário respondê-la."
        );

        try
        {
            await _emailService.SendAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send status change email of service order [{ServiceOrderId}] to customer [{CustomerId}].",
                domainEvent.ServiceOrderId,
                domainEvent.CustomerId
            );
        }
    }

    private static string ToDisplayName(ServiceOrderStatus status) => status switch
    {
        ServiceOrderStatus.Received => "Recebida",
        ServiceOrderStatus.Diagnosing => "Em diagnóstico",
        ServiceOrderStatus.AwaitingApproval => "Aguardando aprovação",
        ServiceOrderStatus.InProgress => "Em execução",
        ServiceOrderStatus.Completed => "Finalizada",
        ServiceOrderStatus.Delivered => "Entregue",
        ServiceOrderStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };
}
