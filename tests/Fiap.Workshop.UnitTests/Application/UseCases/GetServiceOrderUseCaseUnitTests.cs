using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrder.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetServiceOrderUseCaseUnitTests : LoggerTestBase<GetServiceOrderUseCase>
{
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetServiceOrderUseCase _useCase;

    public GetServiceOrderUseCaseUnitTests()
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

    [Fact(DisplayName = "Handle >> Should Success >> When service order exists")]
    public async Task Handle_ShouldSuccess_WhenServiceOrderExists()
    {
        // Arrange
        var serviceOrder = CreateServiceOrder(_fixture.Create<Guid>());
        var input = new GetServiceOrderInput(_fixture.Create<Guid>(), serviceOrder.Id);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        var response = result.Result.Should().BeOfType<ServiceOrderResponse>().Which;
        response.Id.Should().Be(serviceOrder.Id);
        response.CustomerId.Should().Be(serviceOrder.CustomerId);
        response.Parts.Should().BeEmpty();
        response.Services.Should().BeEmpty();
        response.StatusHistory.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = new GetServiceOrderInput(_fixture.Create<Guid>(), _fixture.Create<Guid>());

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().ContainSingle().Which.Should().Be("Unable to find service order");
        result.Result.Should().BeNull();
        VerifyLog(
            LogLevel.Warning,
            $"[{input.CorrelationId}] | Unable to find service order by [{input.ServiceOrderId}].",
            Times.Once()
        );
    }
}
