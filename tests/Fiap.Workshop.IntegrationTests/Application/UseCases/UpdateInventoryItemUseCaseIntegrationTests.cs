using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;
using Fiap.Workshop.Application.UseCases.InventoryItems.UpdateInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class UpdateInventoryItemUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateInventoryItemInput CreateInventoryItemInput() => new(
        Guid.NewGuid(),
        TestData.ShortString(20),
        "Brake Pad",
        "Front brake pad set",
        100,
        10,
        49.90m,
        UnitOfMeasure.Piece
    );

    private async Task<InventoryItemResponse> CreateInventoryItemAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateInventoryItemUseCase>();
        var result = await createUseCase.Handle(CreateInventoryItemInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<InventoryItemResponse>().Subject;
    }

    private async Task<Output> HandleAsync(UpdateInventoryItemInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateInventoryItemUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "UpdateInventoryItemUseCase >> Should persist changes >> When inventory item exists")]
    public async Task Handle_ShouldPersistChanges_WhenInventoryItemExists()
    {
        // Arrange
        var created = await CreateInventoryItemAsync();
        var input = new UpdateInventoryItemInput(Guid.NewGuid(), created.Id, "Updated Name", "Updated Description", 59.90m, 20, UnitOfMeasure.Liter, false);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<InventoryItemResponse>().Subject;
        updated.Id.Should().Be(created.Id);
        updated.Code.Should().Be(created.Code);

        using var scope = fixture.Services.CreateScope();
        var inventoryItemRepository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        var found = await inventoryItemRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Updated Name");
        found.Description.Should().Be("Updated Description");
        found.UnitPrice.Should().Be(59.90m);
        found.MinimumStock.Should().Be(20);
        found.UnitOfMeasure.Should().Be(UnitOfMeasure.Liter);
        found.IsActive.Should().BeFalse();
        found.Code.Should().Be(created.Code);
        found.QuantityOnHand.Should().Be(created.QuantityOnHand);
        found.ReservedQuantity.Should().Be(created.ReservedQuantity);
    }

    [Fact(DisplayName = "UpdateInventoryItemUseCase >> Should fail >> When inventory item does not exist")]
    public async Task Handle_ShouldFail_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var input = new UpdateInventoryItemInput(Guid.NewGuid(), Guid.NewGuid(), "Ghost Item", "Ghost Description", 10m, 5, UnitOfMeasure.Piece, true);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find inventory item");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "UpdateInventoryItemUseCase >> Should throw >> When unit price is negative")]
    public async Task Handle_ShouldThrow_WhenUnitPriceIsNegative()
    {
        // Arrange
        var created = await CreateInventoryItemAsync();
        var input = new UpdateInventoryItemInput(Guid.NewGuid(), created.Id, created.Name, created.Description, -1m, 10, UnitOfMeasure.Piece, true);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }
}
