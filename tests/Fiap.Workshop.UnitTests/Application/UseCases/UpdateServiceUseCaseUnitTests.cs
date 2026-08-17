using AutoFixture;
using Fiap.Workshop.Application.DTOs.Service;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.Services.UpdateService;
using Fiap.Workshop.Application.UseCases.Services.UpdateService.Boundaries;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class UpdateServiceUseCaseUnitTests : LoggerTestBase<UpdateServiceUseCase>
{
    private readonly Mock<IServiceRepository> _serviceRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly UpdateServiceUseCase _useCase;

    public UpdateServiceUseCaseUnitTests()
    {
        _useCase = new(
            _serviceRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private UpdateServiceInput CreateInput(Guid serviceId) => new(
        _fixture.Create<Guid>(),
        serviceId,
        "New Name",
        "New Description",
        200.00m,
        45,
        false
    );

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
        var input = CreateInput(service.Id);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        _serviceRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<ServiceResponse>().Which;
        response.Id.Should().Be(service.Id);
        response.Name.Should().Be("New Name");
        response.IsActive.Should().BeFalse();
        response.Code.Should().Be("SV-001");

        _serviceRepositoryMock.Verify(x => x.Update(service), Times.Once());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service does not exist")]
    public async Task Handle_ShouldFail_WhenServiceDoesNotExist()
    {
        // Arrange
        var input = CreateInput(_fixture.Create<Guid>());

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Service?)null);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find service by [{input.ServiceId}].",
            Times.Once()
        );
        _serviceRepositoryMock.Verify(x => x.Update(It.IsAny<Service>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Throw >> When base price is negative")]
    public async Task Handle_ShouldThrow_WhenBasePriceIsNegative()
    {
        // Arrange
        var service = CreateService(_fixture.Create<Guid>());
        var input = new UpdateServiceInput(_fixture.Create<Guid>(), service.Id, "New Name", "New Description", -0.01m, 45, true);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        // Act
        var act = async () => await _useCase.Handle(input, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DomainException>();
        _serviceRepositoryMock.Verify(x => x.Update(It.IsAny<Service>()), Times.Never());
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When error updating service")]
    public async Task Handle_ShouldFail_WhenErrorUpdatingService()
    {
        // Arrange
        var service = CreateService(_fixture.Create<Guid>());
        var input = CreateInput(service.Id);

        _serviceRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        _serviceRepositoryMock.Setup(x => x.UnitOfWork.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be($"Error updating service with id {input.ServiceId}.");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Error,
            $"[{input.CorrelationId}] | Error updating service with id {input.ServiceId}.",
            Times.Once()
        );
    }
}
