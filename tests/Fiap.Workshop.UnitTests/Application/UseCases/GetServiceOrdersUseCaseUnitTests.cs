using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders.Mapper;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetServiceOrdersUseCaseUnitTests : LoggerTestBase<GetServiceOrdersUseCase>
{
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetServiceOrdersUseCase _useCase;

    public GetServiceOrdersUseCaseUnitTests()
    {
        _useCase = new(
            _serviceOrderRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static ServiceOrder CreateServiceOrder(Guid id) => new(
        id,
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Engine noise",
        12000,
        DateTime.Now,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "Handle >> Should Success >> When service orders exist")]
    public async Task Handle_ShouldSuccess_WhenServiceOrdersExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();
        var serviceOrders = new List<ServiceOrder> { CreateServiceOrder(_fixture.Create<Guid>()) };

        _serviceOrderRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync(serviceOrders);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeEquivalentTo(serviceOrders.MapToDto());
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle >> Should Success and log >> When service orders do not exist")]
    public async Task Handle_ShouldSuccessAndLog_WhenServiceOrdersDoNotExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();

        _serviceOrderRepositoryMock.Setup(x => x.GetAllAsync(CancellationToken.None))
            .ReturnsAsync([]);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeAssignableTo<IEnumerable<ServiceOrderResponse>>()
            .Which.Should().BeEmpty();
        result.ErrorMessages.Should().BeEmpty();
        VerifyLog(
            LogLevel.Warning,
            $"[{correlation}] | Unable to find service orders",
            Times.Once()
        );
    }
}
