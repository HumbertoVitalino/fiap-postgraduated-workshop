using AutoFixture;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItem;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetInventoryItemUseCaseUnitTests : LoggerTestBase<GetInventoryItemUseCase>
{
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetInventoryItemUseCase _useCase;

    public GetInventoryItemUseCaseUnitTests()
    {
        _useCase = new(
            _inventoryItemRepositoryMock.Object,
            LoggerMock.Object
        );
    }

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
        var correlationId = _fixture.Create<Guid>();

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(inventoryItem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);

        // Act
        var result = await _useCase.Handle(new GetInventoryItemInput(correlationId, inventoryItem.Id), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<InventoryItemResponse>().Which;
        response.Id.Should().Be(inventoryItem.Id);
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When inventory item does not exist")]
    public async Task Handle_ShouldFail_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var inventoryItemId = _fixture.Create<Guid>();
        var correlationId = _fixture.Create<Guid>();

        _inventoryItemRepositoryMock.Setup(x => x.GetByIdAsync(inventoryItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        // Act
        var result = await _useCase.Handle(new GetInventoryItemInput(correlationId, inventoryItemId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find inventory item");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{correlationId}] | Unable to find inventory item by [{inventoryItemId}].",
            Times.Once()
        );
    }
}
