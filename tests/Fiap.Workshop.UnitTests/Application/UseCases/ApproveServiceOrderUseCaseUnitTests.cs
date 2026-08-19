using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ApproveServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ApproveServiceOrder.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class ApproveServiceOrderUseCaseUnitTests : LoggerTestBase<ApproveServiceOrderUseCase>
{
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly ApproveServiceOrderUseCase _useCase;

    public ApproveServiceOrderUseCaseUnitTests()
    {
        _useCase = new(
            _serviceOrderRepositoryMock.Object,
            _inventoryItemRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static ServiceOrder CreateAwaitingApprovalServiceOrder(Guid id, Guid inventoryItemId, int quantity)
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

        var part = new ServiceOrderPart(
            Guid.NewGuid(), serviceOrder.Id, inventoryItemId, "Brake Pad", "Brake pad set",
            49.90m, quantity, DateTime.Now, DateTime.Now);

        serviceOrder.AddBudget([], [part], Guid.NewGuid());

        return serviceOrder;
    }

    private static ServiceOrder CreateAwaitingApprovalServiceOrderWithNoParts(Guid id)
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

        return serviceOrder;
    }

    private static InventoryItem CreateInventoryItem(Guid id, int quantityOnHand, int reservedQuantity) => new(
        id,
        "BP-001",
        "Brake Pad",
        "Brake pad set",
        quantityOnHand,
        reservedQuantity,
        5,
        49.90m,
        UnitOfMeasure.Piece,
        true,
        DateTime.Now,
        DateTime.Now
    );

    private ApproveServiceOrderInput CreateInput(Guid serviceOrderId) => new(
        _fixture.Create<Guid>(),
        serviceOrderId,
        _fixture.Create<Guid>()
    );

    [Fact(DisplayName = "Handle >> Should Success >> When service order is AwaitingApproval and all reserved parts exist")]
    public async Task Handle_ShouldSuccess_WhenServiceOrderIsAwaitingApprovalAndAllReservedPartsExist()
    {
        // Arrange
        var inventoryItemId = _fixture.Create<Guid>();
        var serviceOrder = CreateAwaitingApprovalServiceOrder(_fixture.Create<Guid>(), inventoryItemId, 3);
        var inventoryItem = CreateInventoryItem(inventoryItemId, 20, 8);
        var input = CreateInput(serviceOrder.Id);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(inventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        _serviceOrderRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<ServiceOrderResponse>().Which;
        response.Status.Should().Be(ServiceOrderStatus.InProgress.ToString());

        inventoryItem.QuantityOnHand.Should().Be(17);
        inventoryItem.ReservedQuantity.Should().Be(5);

        _inventoryItemRepositoryMock.Verify(x => x.Update(inventoryItem), Times.Once());
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

    [Fact(DisplayName = "Handle >> Should Fail >> When inventory item does not exist")]
    public async Task Handle_ShouldFail_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var inventoryItemId = _fixture.Create<Guid>();
        var serviceOrder = CreateAwaitingApprovalServiceOrder(_fixture.Create<Guid>(), inventoryItemId, 3);
        var input = CreateInput(serviceOrder.Id);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(inventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find inventory item");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find inventory item by [{inventoryItemId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving service order")]
    public async Task Handle_ShouldFail_WhenErrorSavingServiceOrder()
    {
        // Arrange
        var serviceOrder = CreateAwaitingApprovalServiceOrderWithNoParts(_fixture.Create<Guid>());
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
