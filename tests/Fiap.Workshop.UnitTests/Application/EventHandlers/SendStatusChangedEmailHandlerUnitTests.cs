using Fiap.Workshop.Application.EventHandlers.ServiceOrders;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.Services;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Events;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.EventHandlers;

public class SendStatusChangedEmailHandlerUnitTests : LoggerTestBase<SendStatusChangedEmailHandler>
{
    private readonly Mock<ICustomerRepository> _customerRepositoryMock = new();
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly SendStatusChangedEmailHandler _handler;

    public SendStatusChangedEmailHandlerUnitTests()
    {
        _handler = new(
            _customerRepositoryMock.Object,
            _emailServiceMock.Object,
            LoggerMock.Object
        );
    }

    private static Customer CreateCustomer() => new(
        Guid.NewGuid(),
        "Maria Silva",
        "12345678909",
        "maria@example.com",
        "11999999999",
        DateTime.Now,
        DateTime.Now
    );

    private static ServiceOrderStatusChangedEvent CreateEvent(
        Guid customerId,
        ServiceOrderStatus previousStatus = ServiceOrderStatus.AwaitingApproval,
        ServiceOrderStatus currentStatus = ServiceOrderStatus.InProgress) => new(
        Guid.NewGuid(),
        customerId,
        previousStatus,
        currentStatus,
        new DateTime(2026, 10, 26, 14, 32, 0)
    );

    [Fact(DisplayName = "HandleAsync >> Should send email to customer >> When customer exists")]
    public async Task HandleAsync_ShouldSendEmailToCustomer_WhenCustomerExists()
    {
        // Arrange
        var customer = CreateCustomer();
        var domainEvent = CreateEvent(customer.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        await _handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(x => x.SendAsync(
            It.Is<EmailMessage>(message =>
                message.To == customer.Email &&
                message.Subject == "Sua ordem de serviço agora está \"Em execução\"" &&
                message.Body.Contains("Olá, Maria Silva.") &&
                message.Body.Contains("mudou de \"Aguardando aprovação\" para \"Em execução\" em 26/10/2026 14:32.")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory(DisplayName = "HandleAsync >> Should use portuguese status name >> When status changes")]
    [InlineData(ServiceOrderStatus.Received, "Recebida")]
    [InlineData(ServiceOrderStatus.Diagnosing, "Em diagnóstico")]
    [InlineData(ServiceOrderStatus.AwaitingApproval, "Aguardando aprovação")]
    [InlineData(ServiceOrderStatus.InProgress, "Em execução")]
    [InlineData(ServiceOrderStatus.Completed, "Finalizada")]
    [InlineData(ServiceOrderStatus.Delivered, "Entregue")]
    [InlineData(ServiceOrderStatus.Cancelled, "Cancelada")]
    public async Task HandleAsync_ShouldUsePortugueseStatusName_WhenStatusChanges(ServiceOrderStatus status, string displayName)
    {
        // Arrange
        var customer = CreateCustomer();
        var domainEvent = CreateEvent(customer.Id, currentStatus: status);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        await _handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(x => x.SendAsync(
            It.Is<EmailMessage>(message =>
                message.To == customer.Email &&
                message.Subject == $"Sua ordem de serviço agora está \"{displayName}\""),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "HandleAsync >> Should not send and log >> When customer does not exist")]
    public async Task HandleAsync_ShouldNotSendAndLog_WhenCustomerDoesNotExist()
    {
        // Arrange
        var domainEvent = CreateEvent(Guid.NewGuid());

        // Act
        await _handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        _emailServiceMock.Verify(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLog(
            LogLevel.Warning,
            $"Unable to find customer [{domainEvent.CustomerId}] to notify status change of service order [{domainEvent.ServiceOrderId}].",
            Times.Once()
        );
    }

    [Fact(DisplayName = "HandleAsync >> Should log error and not throw >> When sending email fails")]
    public async Task HandleAsync_ShouldLogErrorAndNotThrow_WhenSendingEmailFails()
    {
        // Arrange
        var customer = CreateCustomer();
        var domainEvent = CreateEvent(customer.Id);

        _customerRepositoryMock.Setup(x => x.GetByIdAsync(customer.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        _emailServiceMock.Setup(x => x.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP server unavailable"));

        // Act
        var act = () => _handler.HandleAsync(domainEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
        VerifyLog(
            LogLevel.Error,
            $"Failed to send status change email of service order [{domainEvent.ServiceOrderId}] to customer [{customer.Id}].",
            Times.Once()
        );
    }
}
