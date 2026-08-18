using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.InventoryItem;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.InventoryItems.CreateInventoryItem.Boundaries;
using Fiap.Workshop.Application.UseCases.InventoryItems.GetInventoryItem.Boundaries;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetInventoryItemUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(GetInventoryItemInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetInventoryItemUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetInventoryItemUseCase >> Should return inventory item >> When inventory item exists")]
    public async Task Handle_ShouldReturnInventoryItem_WhenInventoryItemExists()
    {
        // Arrange
        var created = await CreateInventoryItemAsync();
        var input = new GetInventoryItemInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var found = result.Result.Should().BeOfType<InventoryItemResponse>().Subject;
        found.Id.Should().Be(created.Id);
    }

    [Fact(DisplayName = "GetInventoryItemUseCase >> Should fail >> When inventory item does not exist")]
    public async Task Handle_ShouldFail_WhenInventoryItemDoesNotExist()
    {
        // Arrange
        var input = new GetInventoryItemInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find inventory item");
        result.Result.Should().BeNull();
    }
}
