using AutoFixture;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Services.GetService;
using Fiap.Workshop.Application.UseCases.Services.GetService.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetServiceUseCaseUnitTests : LoggerTestBase<GetServiceUseCase>
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetServiceUseCase _useCase;

    public GetServiceUseCaseUnitTests()
    {
        _useCase = new(
            _serviceRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static Service CreateService(Guid id) => new(
        id,
        "SV-001",
        "Oil Change",
        "Oil change service",
        120m,
        30,
        true,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When service exists")]
    public async Task Handle_ShouldSuccess_WhenServiceExists()
    {
        // Arrange
        var service = CreateService(_fixture.Create<Guid>());
        var correlationId = _fixture.Create<Guid>();

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(service.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        // Act
        var result = await _useCase.Handle(new GetServiceInput(correlationId, service.Id), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<ServiceResponse>().Which;
        response.Id.Should().Be(service.Id);
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service does not exist")]
    public async Task Handle_ShouldFail_WhenServiceDoesNotExist()
    {
        // Arrange
        var serviceId = _fixture.Create<Guid>();
        var correlationId = _fixture.Create<Guid>();

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(serviceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        // Act
        var result = await _useCase.Handle(new GetServiceInput(correlationId, serviceId), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{correlationId}] | Unable to find service by [{serviceId}].",
            Times.Once()
        );
    }
}
