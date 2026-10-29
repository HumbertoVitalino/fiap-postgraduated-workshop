using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision;
using Fiap.Workshop.Application.UseCases.ServiceOrders.ProcessBudgetDecision.Boundaries;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class ProcessBudgetDecisionUseCaseUnitTests : LoggerTestBase<ProcessBudgetDecisionUseCase>
{
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly ProcessBudgetDecisionUseCase _useCase;

    public ProcessBudgetDecisionUseCaseUnitTests()
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

    private static ServiceOrder CreateDecidedServiceOrder(Guid id, BudgetDecision decision)
    {
        var serviceOrder = CreateAwaitingApprovalServiceOrderWithNoParts(id);

        if (decision == BudgetDecision.Approved)
            serviceOrder.Approve(Guid.NewGuid());
        else
            serviceOrder.Reject(Guid.NewGuid());

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

    private ProcessBudgetDecisionInput CreateInput(Guid serviceOrderId, BudgetDecision decision) => new(
        _fixture.Create<Guid>(),
        serviceOrderId,
        decision
    );

    [Fact(DisplayName = "Handle >> Should commit stock and move to InProgress >> When decision is Approved and service order is AwaitingApproval")]
    public async Task Handle_ShouldCommitStockAndMoveToInProgress_WhenDecisionIsApprovedAndServiceOrderIsAwaitingApproval()
    {
        // Arrange
        var inventoryItemId = _fixture.Create<Guid>();
        var serviceOrder = CreateAwaitingApprovalServiceOrder(_fixture.Create<Guid>(), inventoryItemId, 3);
        var inventoryItem = CreateInventoryItem(inventoryItemId, 20, 8);
        var input = CreateInput(serviceOrder.Id, BudgetDecision.Approved);

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
        response.StatusHistory.Should().ContainSingle(h => h.CurrentStatus == ServiceOrderStatus.InProgress.ToString())
            .Which.ChangedBy.Should().Be(ProcessBudgetDecisionUseCase.SystemUserId);

        inventoryItem.QuantityOnHand.Should().Be(17);
        inventoryItem.ReservedQuantity.Should().Be(5);

        _inventoryItemRepositoryMock.Verify(x => x.Update(inventoryItem), Times.Once());
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(serviceOrder, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should release stock and move to Cancelled >> When decision is Rejected and service order is AwaitingApproval")]
    public async Task Handle_ShouldReleaseStockAndMoveToCancelled_WhenDecisionIsRejectedAndServiceOrderIsAwaitingApproval()
    {
        // Arrange
        var inventoryItemId = _fixture.Create<Guid>();
        var serviceOrder = CreateAwaitingApprovalServiceOrder(_fixture.Create<Guid>(), inventoryItemId, 3);
        var inventoryItem = CreateInventoryItem(inventoryItemId, 20, 8);
        var input = CreateInput(serviceOrder.Id, BudgetDecision.Rejected);

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
        response.Status.Should().Be(ServiceOrderStatus.Cancelled.ToString());
        response.StatusHistory.Should().ContainSingle(h => h.CurrentStatus == ServiceOrderStatus.Cancelled.ToString())
            .Which.ChangedBy.Should().Be(ProcessBudgetDecisionUseCase.SystemUserId);

        inventoryItem.QuantityOnHand.Should().Be(20);
        inventoryItem.ReservedQuantity.Should().Be(5);

        _inventoryItemRepositoryMock.Verify(x => x.Update(inventoryItem), Times.Once());
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(serviceOrder, It.IsAny<CancellationToken>()), Times.Once());
    }

    [Theory(DisplayName = "Handle >> Should succeed without changes >> When the same decision was already applied")]
    [InlineData(BudgetDecision.Approved)]
    [InlineData(BudgetDecision.Rejected)]
    public async Task Handle_ShouldSucceedWithoutChanges_WhenTheSameDecisionWasAlreadyApplied(BudgetDecision decision)
    {
        // Arrange
        var serviceOrder = CreateDecidedServiceOrder(_fixture.Create<Guid>(), decision);
        var input = CreateInput(serviceOrder.Id, decision);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Messages.Should().ContainSingle().Which.Should().Be("Budget decision already applied");
        result.Result.Should().BeOfType<ServiceOrderResponse>().Which.Status.Should().Be(serviceOrder.Status.ToString());
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Budget decision {decision} already applied to service order [{input.ServiceOrderId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
        _serviceOrderRepositoryMock.Verify(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()), Times.Never());
    }

    [Theory(DisplayName = "Handle >> Should throw >> When the opposite decision was already applied")]
    [InlineData(BudgetDecision.Approved, BudgetDecision.Rejected)]
    [InlineData(BudgetDecision.Rejected, BudgetDecision.Approved)]
    public async Task Handle_ShouldThrow_WhenTheOppositeDecisionWasAlreadyApplied(BudgetDecision appliedDecision, BudgetDecision decision)
    {
        // Arrange
        var serviceOrder = CreateDecidedServiceOrder(_fixture.Create<Guid>(), appliedDecision);
        var input = CreateInput(serviceOrder.Id, decision);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        // Act
        var act = async () => await _useCase.Handle(input, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _inventoryItemRepositoryMock.Verify(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
        _serviceOrderRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<ServiceOrder>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>(), BudgetDecision.Approved);

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
        var input = CreateInput(serviceOrder.Id, BudgetDecision.Approved);

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
        var input = CreateInput(serviceOrder.Id, BudgetDecision.Approved);

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
