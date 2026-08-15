using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetInventoryItemsUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(Guid input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetInventoryItemsUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetInventoryItemsUseCase >> Should return inventory item >> When inventory items exist")]
    public async Task GetInventoryItemsUseCase_ShouldReturnInventoryItems_WhenInventoryItemsExist()
    {
        // Arrange
        var created = await CreateInventoryItemAsync();

        // Act
        var result = await HandleAsync(Guid.NewGuid());

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var inventoryItems = result.Result.Should().BeAssignableTo<IEnumerable<InventoryItemResponse>>().Subject;
        inventoryItems.Should().ContainSingle(i => i.Id == created.Id)
            .Which.Should().BeEquivalentTo(created);
    }
}
