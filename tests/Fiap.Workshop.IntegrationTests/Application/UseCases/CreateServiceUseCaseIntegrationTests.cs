using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class CreateServiceUseCaseIntegrationTests(DatabaseFixture fixture)
{
    private static CreateServiceInput CreateServiceInput(string? code = null) => new(
        Guid.NewGuid(),
        code ?? TestData.ShortString(20),
        "Oil Change",
        "Engine oil and filter replacement",
        150.00m,
        60
    );

    private async Task<Output> HandleAsync(CreateServiceInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<ICreateServiceUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "CreateServiceUseCase >> Should persist service >> When code is not in use")]
    public async Task Handle_ShouldPersistService_WhenCodeIsNotInUse()
    {
        // Arrange
        var input = CreateServiceInput();

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var created = result.Result.Should().BeOfType<ServiceResponse>().Subject;
        created.Code.Should().Be(input.Code);
        created.IsActive.Should().BeTrue();

        using var scope = fixture.Services.CreateScope();
        var serviceRepository = scope.ServiceProvider.GetRequiredService<IServiceRepository>();
        var found = await serviceRepository.GetByIdAsync(created.Id, CancellationToken.None);
        found.Should().NotBeNull();
        found!.Code.Should().Be(input.Code);
        found.BasePrice.Should().Be(input.BasePrice);
        found.EstimatedDuration.Should().Be(input.EstimatedDuration);
    }

    [Fact(DisplayName = "CreateServiceUseCase >> Should fail >> When code already exists")]
    public async Task Handle_ShouldFail_WhenCodeAlreadyExists()
    {
        // Arrange
        var firstInput = CreateServiceInput();
        await HandleAsync(firstInput);

        var duplicateInput = CreateServiceInput(firstInput.Code);

        // Act
        var result = await HandleAsync(duplicateInput);

        // Assert
        result.ErrorMessages.Should().ContainSingle()
            .Which.Should().Be($"Service with code {firstInput.Code} already exists.");
        result.Result.Should().BeNull();
    }
}
