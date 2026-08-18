using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.UseCases;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;
using Fiap.Workshop.Application.UseCases.Services.GetService.Boundaries;
using Fiap.Workshop.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Fiap.Workshop.IntegrationTests.Application.UseCases;

[Collection("Integration")]
public sealed class GetServiceUseCaseIntegrationTests(DatabaseFixture fixture)
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

    private async Task<Output> HandleAsync(GetServiceInput input)
    {
        using var scope = fixture.Services.CreateScope();
        var useCase = scope.ServiceProvider.GetRequiredService<IGetServiceUseCase>();
        return await useCase.Handle(input, CancellationToken.None);
    }

    [Fact(DisplayName = "GetServiceUseCase >> Should return service >> When service exists")]
    public async Task Handle_ShouldReturnService_WhenServiceExists()
    {
        // Arrange
        var created = await CreateServiceAsync();
        var input = new GetServiceInput(Guid.NewGuid(), created.Id);

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        var found = result.Result.Should().BeOfType<ServiceResponse>().Subject;
        found.Id.Should().Be(created.Id);
    }

    [Fact(DisplayName = "GetServiceUseCase >> Should fail >> When service does not exist")]
    public async Task Handle_ShouldFail_WhenServiceDoesNotExist()
    {
        // Arrange
        var input = new GetServiceInput(Guid.NewGuid(), Guid.NewGuid());

        // Act
        var result = await HandleAsync(input);

        // Assert
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service");
        result.Result.Should().BeNull();
    }
}
