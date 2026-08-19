using AutoFixture;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Services.GetServices;
using Fiap.Workshop.Application.UseCases.Services.GetServices.Mapper;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetServicesUseCaseUnitTests : LoggerTestBase<GetServicesUseCase>
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetServicesUseCase _useCase;

    public GetServicesUseCaseUnitTests()
    {
        _useCase = new(
            _serviceRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    [Fact(DisplayName = "Handle >> Should Success >> When services exist")]
    public async Task Handle_ShouldSuccess_WhenServicesExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();
        var services = _fixture.CreateMany<Service>();

        _serviceRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync(services);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeEquivalentTo(services.MapToDto());
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle >> Should Success and log >> When services do not exist")]
    public async Task Handle_ShouldSuccessAndLog_WhenServicesDoNotExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();

        _serviceRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeAssignableTo<IEnumerable<ServiceResponse>>()
            .Which.Should().BeEmpty();
        result.ErrorMessages.Should().BeEmpty();
        VerifyLog(
            LogLevel.Warning,
            $"[{correlation}] | Unable to find services",
            Times.Once()
        );
    }
}
