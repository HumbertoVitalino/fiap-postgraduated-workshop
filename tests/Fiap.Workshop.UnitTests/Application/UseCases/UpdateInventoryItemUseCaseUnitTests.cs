using AutoFixture;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.InventoryItems.UpdateInventoryItem;
using Fiap.Workshop.Application.UseCases.InventoryItems.UpdateInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class UpdateInventoryItemUseCaseUnitTests : LoggerTestBase<UpdateInventoryItemUseCase>
{
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly UpdateInventoryItemUseCase _useCase;

    public UpdateInventoryItemUseCaseUnitTests()
    {
        _useCase = new(
            _inventoryItemRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private UpdateInventoryItemInput CreateInput(Guid inventoryItemId) => new(
        _fixture.Create<Guid>(),
        inventoryItemId,
        "New Name",
        "New Description",
        59.90m,
        10,
        UnitOfMeasure.Liter,
        false
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

    [Fact(DisplayName = "Handle >> Should Success >> When inventory item exists")]
    public async Task Handle_ShouldSuccess_WhenInventoryItemExists()
    {
        // Arrange
        var inventoryItem = CreateInventoryItem(_fixture.Create<Guid>());
        var input = CreateInput(inventoryItem.Id);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(input.InventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        _inventoryItemRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<InventoryItemResponse>().Which;
        response.Id.Should().Be(inventoryItem.Id);
        response.Name.Should().Be("New Name");
        response.IsActive.Should().BeFalse();
        response.Code.Should().Be("BP-001");
        response.QuantityOnHand.Should().Be(20);

        _inventoryItemRepositoryMock.Verify(x => x.Update(inventoryItem), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When inventory item does not exist")]
    public async Task Handle_ShouldFail_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(input.InventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find inventory item");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find inventory item by [{input.InventoryItemId}].",
            Times.Once()
        );
        _inventoryItemRepositoryMock.Verify(x => x.Update(It.IsAny<InventoryItem>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Throw >> When unit price is negative")]
    public async Task Handle_ShouldThrow_WhenUnitPriceIsNegative()
    {
        // Arrange
        var inventoryItem = CreateInventoryItem(_fixture.Create<Guid>());
        var input = new UpdateInventoryItemInput(_fixture.Create<Guid>(), inventoryItem.Id, "New Name", "New Description", -0.01m, 10, UnitOfMeasure.Liter, true);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(input.InventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        // Act
        var act = async () => await _useCase.Handle(input, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _inventoryItemRepositoryMock.Verify(x => x.Update(It.IsAny<InventoryItem>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error updating inventory item")]
    public async Task Handle_ShouldFail_WhenErrorUpdatingInventoryItem()
    {
        // Arrange
        var inventoryItem = CreateInventoryItem(_fixture.Create<Guid>());
        var input = CreateInput(inventoryItem.Id);

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(input.InventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        _inventoryItemRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error updating inventory item with id {input.InventoryItemId}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error updating inventory item with id {input.InventoryItemId}.",
            Times.Once()
        );
    }
}
