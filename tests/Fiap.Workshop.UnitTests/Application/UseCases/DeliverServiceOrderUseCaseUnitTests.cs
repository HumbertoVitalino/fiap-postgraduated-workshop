using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.DeliverServiceOrder.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class DeliverServiceOrderUseCaseUnitTests : LoggerTestBase<DeliverServiceOrderUseCase>
{
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly DeliverServiceOrderUseCase _useCase;

    public DeliverServiceOrderUseCaseUnitTests()
    {
        _useCase = new(
            _serviceOrderRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static ServiceOrder CreateCompletedServiceOrder(Guid id)
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
        serviceOrder.Complete([], Guid.NewGuid());

        return serviceOrder;
    }

    private DeliverServiceOrderInput CreateInput(Guid serviceOrderId) => new(
        _fixture.Create<Guid>(),
        serviceOrderId,
        _fixture.Create<Guid>()
    );

    [Fact(DisplayName = "Handle >> Should Success >> When service order is Completed")]
    public async Task Handle_ShouldSuccess_WhenServiceOrderIsCompleted()
    {
        // Arrange
        var serviceOrder = CreateCompletedServiceOrder(_fixture.Create<Guid>());
        var input = CreateInput(serviceOrder.Id);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _serviceOrderRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<ServiceOrderResponse>().Which;
        response.Status.Should().Be(ServiceOrderStatus.Delivered.ToString());
        response.ClosedAt.Should().NotBeNull();

        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(serviceOrder, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

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

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving service order")]
    public async Task Handle_ShouldFail_WhenErrorSavingServiceOrder()
    {
        // Arrange
        var serviceOrder = CreateCompletedServiceOrder(_fixture.Create<Guid>());
        var input = CreateInput(serviceOrder.Id);

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
