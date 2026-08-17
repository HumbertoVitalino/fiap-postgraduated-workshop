using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.CompleteServiceOrder.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class CompleteServiceOrderUseCaseUnitTests : LoggerTestBase<CompleteServiceOrderUseCase>
{
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly CompleteServiceOrderUseCase _useCase;

    public CompleteServiceOrderUseCaseUnitTests()
    {
        _useCase = new(
            _serviceOrderRepositoryMock.Object,
            _serviceRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static ServiceOrder CreateInProgressServiceOrder(Guid id, Guid serviceId, decimal unitPrice, int quantity)
    {
        var serviceOrder = new ServiceOrder(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Engine noise",
            12000,
            DateTime.Now,
            DateTime.Now,
            DateTime.Now
        );

        serviceOrder.StartDiagnosis("Worn brake pads", Guid.NewGuid());

        var orderService = new ServiceOrderService(
            Guid.NewGuid(), serviceOrder.Id, serviceId, "Oil Change", "Oil change service",
            unitPrice, 30, quantity, DateTime.Now, DateTime.Now);

        serviceOrder.AddBudget([orderService], [], Guid.NewGuid());
        serviceOrder.Approve(Guid.NewGuid());

        return serviceOrder;
    }

    private static ServiceOrder CreateInProgressServiceOrderWithNoServices(Guid id)
    {
        var serviceOrder = new ServiceOrder(
            id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Engine noise",
            12000,
            DateTime.Now,
            DateTime.Now,
            DateTime.Now
        );

        serviceOrder.StartDiagnosis("Worn brake pads", Guid.NewGuid());
        serviceOrder.AddBudget([], [], Guid.NewGuid());
        serviceOrder.Approve(Guid.NewGuid());

        return serviceOrder;
    }

    private static Service CreateService(Guid id, short estimatedDuration) => new(
        id,
        "SV-001",
        "Oil Change",
        "Oil change service",
        120m,
        estimatedDuration,
        true,
        DateTime.Now,
        DateTime.Now
    );

    private CompleteServiceOrderInput CreateInput(Guid serviceOrderId, IReadOnlyCollection<CompleteServiceOrderDuration> durations) => new(
        _fixture.Create<Guid>(),
        serviceOrderId,
        _fixture.Create<Guid>(),
        durations
    );

    [Fact(DisplayName = "Handle >> Should Success >> When service order is InProgress and every service duration is informed")]
    public async Task Handle_ShouldSuccess_WhenServiceOrderIsInProgressAndEveryServiceDurationIsInformed()
    {
        // Arrange
        var serviceId = _fixture.Create<Guid>();
        var serviceOrder = CreateInProgressServiceOrder(_fixture.Create<Guid>(), serviceId, 120m, 1);
        var orderServiceId = serviceOrder.Services.Single().Id;
        var service = CreateService(serviceId, 60);
        var input = CreateInput(serviceOrder.Id, [new CompleteServiceOrderDuration(orderServiceId, 90)]);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        _serviceOrderRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<ServiceOrderResponse>().Which;
        response.Status.Should().Be(ServiceOrderStatus.Completed.ToString());

        service.EstimatedDuration.Should().Be(90);
        service.ExecutionCount.Should().Be(1);

        _serviceRepositoryMock.Verify(x => x.Update(service), Times.Once());
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(serviceOrder, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>(), []);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ServiceOrder?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find service order by [{input.ServiceOrderId}].",
            Times.Once()
        );
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service does not exist")]
    public async Task Handle_ShouldFail_WhenServiceDoesNotExist()
    {
        // Arrange
        var serviceId = _fixture.Create<Guid>();
        var serviceOrder = CreateInProgressServiceOrder(_fixture.Create<Guid>(), serviceId, 120m, 1);
        var orderServiceId = serviceOrder.Services.Single().Id;
        var input = CreateInput(serviceOrder.Id, [new CompleteServiceOrderDuration(orderServiceId, 90)]);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find service by [{serviceId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving service order")]
    public async Task Handle_ShouldFail_WhenErrorSavingServiceOrder()
    {
        // Arrange
        var serviceOrder = CreateInProgressServiceOrderWithNoServices(_fixture.Create<Guid>());
        var input = CreateInput(serviceOrder.Id, []);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _serviceOrderRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error updating service order with id {input.ServiceOrderId}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error updating service order with id {input.ServiceOrderId}.",
            Times.Once()
        );
    }
}
