using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;
using Fiap.Workshop.Application.UseCases.Services.UpdateService.Boundaries;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class UpdateServiceUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateServiceInput CreateServiceInput() => new(
        Guid.NewGuid(),
        TestData.ShortString(20),
        "Oil Change",
        "Engine oil and filter replacement",
        150.00m,
        60
    );

    private async Task<ServiceResponse> CreateServiceAsync()
    {
        using var scope = fixture.Services.CreateScope();
        var createUseCase = scope.ServiceProvider.GetRequiredService<ICreateServiceUseCase>();
        var result = await createUseCase.Handle(CreateServiceInput(), CancellationToken.None);
        return result.Result.Should().BeOfType<ServiceResponse>().Subject;
    }

    private async Task<Output> HandleAsync(UpdateServiceInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IUpdateServiceUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "UpdateServiceUseCase >> Should persist changes >> When service exists")]
    public async Task Handle_ShouldPersistChanges_WhenServiceExists()
    {
        // Arrange
        var created = await CreateServiceAsync();
        var input = new UpdateServiceInput(Guid.NewGuid(), created.Id, "Updated Name", "Updated Description", 199.00m, 45, false);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var updated = result.Result.Should().BeOfType<ServiceResponse>().Subject;
        updated.Id.Should().Be(created.Id);
        updated.Code.Should().Be(created.Code);

        using var scope = fixture.Services.CreateScope();
        var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var found = await serviceRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Name.Should().Be("Updated Name");
        found.Description.Should().Be("Updated Description");
        found.BasePrice.Should().Be(199.00m);
        found.EstimatedDuration.Should().Be(45);
        found.IsActive.Should().BeFalse();
        found.ExecutionCount.Should().Be(0);
        found.Code.Should().Be(created.Code);
    }

    [Fact(DisplayName = "UpdateServiceUseCase >> Should fail >> When service does not exist")]
    public async Task Handle_ShouldFail_WhenServiceDoesNotExist()
    {
        // Arrange
        var input = new UpdateServiceInput(Guid.NewGuid(), Guid.NewGuid(), "Ghost Service", "Ghost Description", 100m, 30, true);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service");
        result.Result.Should().BeNull();
    }

    [Fact(DisplayName = "UpdateServiceUseCase >> Should throw >> When base price is negative")]
    public async Task Handle_ShouldThrow_WhenBasePriceIsNegative()
    {
        // Arrange
        var created = await CreateServiceAsync();
        var input = new UpdateServiceInput(Guid.NewGuid(), created.Id, created.Name, created.Description, -1m, 45, true);

        // Act
        var act = async () => await HandleAsync(input);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
    }
}
