using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetServicesUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(Guid input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetServicesUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetServicesUseCase >> Should return service >> When services exist")]
    public async Task GetServicesUseCase_ShouldReturnServices_WhenServicesExist()
    {
        // Arrange
        var created = await CreateServiceAsync();

        // Act
        var result = await HandleAsync(Guid.NewGuid());

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var services = result.Result.Should().BeAssignableTo<IEnumerable<ServiceResponse>>().Subject;
        services.Should().ContainSingle(s => s.Id == created.Id)
            .Which.Should().BeEquivalentTo(created);
    }
}
