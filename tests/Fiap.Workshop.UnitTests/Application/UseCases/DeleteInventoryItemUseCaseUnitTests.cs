using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.InventoryItems.DeleteInventoryItem;
using Fiap.Workshop.Application.UseCases.InventoryItems.DeleteInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class DeleteInventoryItemUseCaseUnitTests : LoggerTestBase<DeleteInventoryItemUseCase>
{
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepositoryMock = new();
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly DeleteInventoryItemUseCase _useCase;

    public DeleteInventoryItemUseCaseUnitTests()
    {
        _useCase = new(
            _inventoryItemRepositoryMock.Object,
            _serviceOrderRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private DeleteInventoryItemInput CreateInput(Guid inventoryItemId) => new(
        _fixture.Create<Guid>(),
        inventoryItemId
    );

    private static InventoryItem CreateInventoryItem(Guid id) => new(
        id,
        "BP-001",
        "Brake Pad",
        "Brake pad set",
        20,
        0,
        5,
        89.90m,
        UnitOfMeasure.Piece,
        true,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When inventory item exists and has never been used")]
    public async Task Handle_ShouldSuccess_WhenInventoryItemExistsAndHasNeverBeenUsed()
    {
        // Arrange
        var inventoryItem = CreateInventoryItem(_fixture.Create<Guid>());
        var input = CreateInput(inventoryItem.Id);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(input.InventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithInventoryItemIdAsync(inventoryItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _inventoryItemRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        _inventoryItemRepositoryMock.Verify(x => x.Remove(inventoryItem), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Succeed idempotently >> When inventory item does not exist")]
    public async Task Handle_ShouldSucceedIdempotently_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(input.InventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find inventory item by [{input.InventoryItemId}].",
            Times.Once()
        );
        _serviceOrderRepositoryMock.Verify(x => x.ExistsWithInventoryItemIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never());
        _inventoryItemRepositoryMock.Verify(x => x.Remove(It.IsAny<InventoryItem>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When inventory item has been used in a service order")]
    public async Task Handle_ShouldFail_WhenInventoryItemHasBeenUsedInServiceOrder()
    {
        // Arrange
        var inventoryItem = CreateInventoryItem(_fixture.Create<Guid>());
        var input = CreateInput(inventoryItem.Id);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(input.InventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithInventoryItemIdAsync(inventoryItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Inventory item has been used in a service order and cannot be deleted.");
        result.Result.Should().BeNull();

        _inventoryItemRepositoryMock.Verify(x => x.Remove(It.IsAny<InventoryItem>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error deleting inventory item")]
    public async Task Handle_ShouldFail_WhenErrorDeletingInventoryItem()
    {
        // Arrange
        var inventoryItem = CreateInventoryItem(_fixture.Create<Guid>());
        var input = CreateInput(inventoryItem.Id);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(input.InventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        _serviceOrderRepositoryMock.Setup(x => x.ExistsWithInventoryItemIdAsync(inventoryItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _inventoryItemRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error deleting inventory item with id {input.InventoryItemId}.");
        result.Result.Should().BeNull();

        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error deleting inventory item with id {input.InventoryItemId}.",
            Times.Once()
        );
    }
}
