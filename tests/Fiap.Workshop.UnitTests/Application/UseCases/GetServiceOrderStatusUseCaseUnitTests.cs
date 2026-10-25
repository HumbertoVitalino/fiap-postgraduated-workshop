using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrderStatus.Boundaries;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.UnitTests.Common;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Fiap.Workshop.UnitTests.Application.UseCases;

public class GetServiceOrderStatusUseCaseUnitTests : LoggerTestBase<GetServiceOrderStatusUseCase>
{
    private readonly Mock<IServiceOrderRepository> _serviceOrderRepositoryMock = new();
    private readonly Fixture _fixture = new();
    private readonly GetServiceOrderStatusUseCase _useCase;

    public GetServiceOrderStatusUseCaseUnitTests()
    {
        _useCase = new(
            _serviceOrderRepositoryMock.Object,
            LoggerMock.Object
        );
    }

    private static ServiceOrder CreateServiceOrder(Guid id, DateTime openedAt, ServiceOrderStatus status = ServiceOrderStatus.Received) => new(
        id,
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Engine noise",
        12000,
        DateTime.Now,
        DateTime.Now,
        openedAt,
        status
    );

    [Fact(DisplayName = "Handle >> Should return opening date >> When service order has no status history")]
    public async Task Handle_ShouldReturnOpeningDate_WhenServiceOrderHasNoStatusHistory()
    {
        // Arrange
        var openedAt = DateTime.Now.AddDays(-2);
        var serviceOrder = CreateServiceOrder(_fixture.Create<Guid>(), openedAt);
        var input = new GetServiceOrderStatusInput(_fixture.Create<Guid>(), serviceOrder.Id);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeEquivalentTo(
            new ServiceOrderStatusResponse(serviceOrder.Id, ServiceOrderStatus.Received.ToString(), openedAt));
    }

    [Fact(DisplayName = "Handle >> Should return latest status change date >> When service order has status history")]
    public async Task Handle_ShouldReturnLatestStatusChangeDate_WhenServiceOrderHasStatusHistory()
    {
        // Arrange
        var openedAt = DateTime.Now.AddDays(-3);
        var diagnosingAt = openedAt.AddDays(1);
        var awaitingApprovalAt = openedAt.AddDays(2);
        var serviceOrder = CreateServiceOrder(_fixture.Create<Guid>(), openedAt, ServiceOrderStatus.AwaitingApproval);
        serviceOrder.AddStatusHistory([
            new ServiceOrderStatusHistory(
                Guid.NewGuid(), serviceOrder.Id, ServiceOrderStatus.Diagnosing, ServiceOrderStatus.AwaitingApproval,
                Guid.NewGuid(), awaitingApprovalAt),
            new ServiceOrderStatusHistory(
                Guid.NewGuid(), serviceOrder.Id, ServiceOrderStatus.Received, ServiceOrderStatus.Diagnosing,
                Guid.NewGuid(), diagnosingAt)
        ]);
        var input = new GetServiceOrderStatusInput(_fixture.Create<Guid>(), serviceOrder.Id);

        _serviceOrderRepositoryMock.Setup(x => x.GetByIdAsync(input.ServiceOrderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(serviceOrder);

        // Act
        var result = await _useCase.Handle(input, CancellationToken.None);

        // Assert
        result.ErrorMessages.Should().BeEmpty();
        result.Result.Should().BeEquivalentTo(
            new ServiceOrderStatusResponse(serviceOrder.Id, ServiceOrderStatus.AwaitingApproval.ToString(), awaitingApprovalAt));
    }

    [Fact(DisplayName = "Handle >> Should Fail >> When service order does not exist")]
    public async Task Handle_ShouldFail_WhenServiceOrderDoesNotExist()
    {
        // Arrange
        var input = new GetServiceOrderStatusInput(_fixture.Create<Guid>(), _fixture.Create<Guid>());

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
