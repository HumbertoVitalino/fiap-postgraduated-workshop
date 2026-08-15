using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class CreateInventoryItemUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateInventoryItemInput CreateInventoryItemInput(string? code = null) => new(
        Guid.NewGuid(),
        code ?? TestData.ShortString(20),
        "Brake Pad",
        "Front brake pad set",
        100,
        10,
        49.90m,
        UnitOfMeasure.Piece
    );

    private async Task<Output> HandleAsync(CreateInventoryItemInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateInventoryItemUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "CreateInventoryItemUseCase >> Should persist item >> When code is not in use")]
    public async Task Handle_ShouldPersistItem_WhenCodeIsNotInUse()
    {
        // Arrange
        var input = CreateInventoryItemInput();

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var created = result.Result.Should().BeOfType<InventoryItemResponse>().Subject;
        created.Code.Should().Be(input.Code);
        created.ReservedQuantity.Should().Be(0);
        created.IsActive.Should().BeTrue();

        using var scope = fixture.Services.CreateScope();
        var inventoryItemRepository = scope.ServiceProvider.GetRequiredService<IInventoryItemRepository>();
        var found = await inventoryItemRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Code.Should().Be(input.Code);
        found.QuantityOnHand.Should().Be(input.QuantityOnHand);
        found.ReservedQuantity.Should().Be(0);
        found.MinimumStock.Should().Be(input.MinimumStock);
    }

    [Fact(DisplayName = "CreateInventoryItemUseCase >> Should fail >> When code already exists")]
    public async Task Handle_ShouldFail_WhenCodeAlreadyExists()
    {
        // Arrange
        var firstInput = CreateInventoryItemInput();
        await HandleAsync(firstInput);

        var duplicateInput = CreateInventoryItemInput(firstInput.Code);

        // Act
        var result = await HandleAsync(duplicateInput);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be($"Inventory item with code {firstInput.Code} already exists.");
        result.Result.Should().BeNull();
    }
}
