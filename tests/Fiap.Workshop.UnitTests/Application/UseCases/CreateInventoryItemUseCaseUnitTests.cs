using AutoFixture;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class CreateInventoryItemUseCaseUnitTests : LoggerTestBase<CreateInventoryItemUseCase>
{
    private readonly Mock<IInventoryItemRepository> _inventoryItemRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly CreateInventoryItemUseCase _useCase;

    public CreateInventoryItemUseCaseUnitTests()
    {
        _useCase = new(
            _inventoryItemRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private CreateInventoryItemInput CreateInput() => new(
        _fixture.Create<Guid>(),
        _fixture.Create<string>(),
        "Brake Pad",
        "Front brake pad set",
        100,
        10,
        49.90m,
        UnitOfMeasure.Piece
    );

    [Fact(DisplayName = "Handle >> Should Success >> When code is not in use")]
    public async Task Handle_ShouldSuccess_WhenCodeIsNotInUse()
    {
        // Arrange
        var input = CreateInput();

        _inventoryItemRepositoryMock.Setup(x => x.ExistsWithCodeAsync(input.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _inventoryItemRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().NotBeNull();

        _inventoryItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When code already exists")]
    public async Task Handle_ShouldFail_WhenCodeAlreadyExists()
    {
        // Arrange
        var input = CreateInput();

        _inventoryItemRepositoryMock.Setup(x => x.ExistsWithCodeAsync(input.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Inventory item with code {input.Code} already exists.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Inventory item with code {input.Code} already exists.",
            Times.Once()
        );
        _inventoryItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error saving inventory item")]
    public async Task Handle_ShouldFail_WhenErrorSavingInventoryItem()
    {
        // Arrange
        var input = CreateInput();

        _inventoryItemRepositoryMock.Setup(x => x.ExistsWithCodeAsync(input.Code, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _inventoryItemRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error saving inventory item with code {input.Code}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error saving inventory item with code {input.Code}.",
            Times.Once()
        );
    }
}
