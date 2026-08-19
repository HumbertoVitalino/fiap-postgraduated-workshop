using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItems;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItems.Mapper;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetInventoryItemsUseCaseUnitTests : LoggerTestBase<GetInventoryItemsUseCase>
{
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepositoryMock = new();
    private readonly GetInventoryItemsUseCase _useCase;

    public GetInventoryItemsUseCaseUnitTests()
    {
        _useCase = new(
            _inventoryItemRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static InventoryItem CreateInventoryItem() => new(
        Guid.NewGuid(),
        Guid.NewGuid().ToString("N"),
        "Brake Pad",
        "Front brake pad set",
        100,
        10,
        5,
        49.90m,
        UnitOfMeasure.Piece,
        true,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When inventory items exist")]
    public async Task Handle_ShouldSuccess_WhenInventoryItemsExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();
        var inventoryItems = new[] { CreateInventoryItem(), CreateInventoryItem() };

        _inventoryItemRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync(inventoryItems);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeEquivalentTo(inventoryItems.MapToDto());
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle >> Should Success and log >> When inventory items do not exist")]
    public async Task Handle_ShouldSuccessAndLog_WhenInventoryItemsDoNotExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();

        _inventoryItemRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeAssignableTo<IEnumerable<InventoryItemResponse>>()
            .Which.Should().BeEmpty();
        result.ErrorMessages.Should().BeEmpty();
        VerifyLog(
            LogLevel.Warning,
            $"[{correlation}] | Unable to find inventory items",
            Times.Once()
        );
    }
}
