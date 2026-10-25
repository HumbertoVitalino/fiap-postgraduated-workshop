using AutoFixture;
using Fiap.Workshop.Application.DTOs.ServiceOrder;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.GetServiceOrders.Mapper;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
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

    private static ServiceOrder CreateServiceOrder(Guid id, ServiceOrderStatus status = ServiceOrderStatus.Received, DateTime? openedAt = null) => new(
        id,
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Engine noise",
        12000,
        DateTime.Now,
        DateTime.Now,
        openedAt ?? DateTime.Now,
        status
    );

    [Fact(DisplayName = "Handle >> Should Success >> When service orders exist")]
    public async Task Handle_ShouldSuccess_WhenServiceOrdersExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();
        var serviceOrders = new List<ServiceOrder> { CreateServiceOrder(_fixture.Create<Guid>()) };

        _serviceOrderRepositoryMock.Setup(x => x.GetByStatusesAsync(It.IsAny<IReadOnlyCollection<ServiceOrderStatus>>(), CancellationToken.None))
            .ReturnsAsync(serviceOrders);

        // Act
        var result = await _useCase.Handle(correlation, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeEquivalentTo(serviceOrders.MapToDto());
        result.ErrorMessages.Should().BeEmpty();
    }

    [Fact(DisplayName = "Handle >> Should request only listable statuses >> When listing service orders")]
    public async Task Handle_ShouldRequestOnlyListableStatuses_WhenListingServiceOrders()
    {
        // Arrange
        IReadOnlyCollection<ServiceOrderStatus>? requested = null;

        _serviceOrderRepositoryMock.Setup(x => x.GetByStatusesAsync(It.IsAny<IReadOnlyCollection<ServiceOrderStatus>>(), CancellationToken.None))
            .Callback<IReadOnlyCollection<ServiceOrderStatus>, CancellationToken>((statuses, _) => requested = statuses)
            .ReturnsAsync([]);

        // Act
        await _useCase.Handle(Guid.NewGuid(), CancellationToken.None);

        // Assert
        requested.Should().BeEquivalentTo([
            ServiceOrderStatus.InProgress,
            ServiceOrderStatus.AwaitingApproval,
            ServiceOrderStatus.Diagnosing,
            ServiceOrderStatus.Received,
            ServiceOrderStatus.Cancelled
        ]);
        requested.Should().NotContain([ServiceOrderStatus.Completed, ServiceOrderStatus.Delivered]);
    }

    [Fact(DisplayName = "Handle >> Should order by status priority then oldest first >> When service orders exist")]
    public async Task Handle_ShouldOrderByStatusPriorityThenOldestFirst_WhenServiceOrdersExist()
    {
        // Arrange
        var now = DateTime.Now;
        var receivedOld = CreateServiceOrder(Guid.NewGuid(), ServiceOrderStatus.Received, now.AddDays(-3));
        var receivedNew = CreateServiceOrder(Guid.NewGuid(), ServiceOrderStatus.Received, now.AddDays(-1));
        var cancelled = CreateServiceOrder(Guid.NewGuid(), ServiceOrderStatus.Cancelled, now.AddDays(-10));
        var diagnosing = CreateServiceOrder(Guid.NewGuid(), ServiceOrderStatus.Diagnosing, now.AddDays(-2));
        var awaitingApproval = CreateServiceOrder(Guid.NewGuid(), ServiceOrderStatus.AwaitingApproval, now);
        var inProgressNew = CreateServiceOrder(Guid.NewGuid(), ServiceOrderStatus.InProgress, now.AddHours(-1));
        var inProgressOld = CreateServiceOrder(Guid.NewGuid(), ServiceOrderStatus.InProgress, now.AddDays(-5));

        _serviceOrderRepositoryMock.Setup(x => x.GetByStatusesAsync(It.IsAny<IReadOnlyCollection<ServiceOrderStatus>>(), CancellationToken.None))
            .ReturnsAsync([receivedNew, cancelled, inProgressNew, diagnosing, receivedOld, awaitingApproval, inProgressOld]);

        // Act
        var result = await _useCase.Handle(Guid.NewGuid(), CancellationToken.None);

        // Assert
        result.Result.Should().BeAssignableTo<IEnumerable<ServiceOrderResponse>>()
            .Which.Select(so => so.Id).Should().Equal(
                inProgressOld.Id,
                inProgressNew.Id,
                awaitingApproval.Id,
                diagnosing.Id,
                receivedOld.Id,
                receivedNew.Id,
                cancelled.Id
            );
    }

    [Fact(DisplayName = "Handle >> Should Success and log >> When service orders do not exist")]
    public async Task Handle_ShouldSuccessAndLog_WhenServiceOrdersDoNotExist()
    {
        // Arrange
        var correlation = Guid.NewGuid();

        _serviceOrderRepositoryMock.Setup(x => x.GetByStatusesAsync(It.IsAny<IReadOnlyCollection<ServiceOrderStatus>>(), CancellationToken.None))
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
