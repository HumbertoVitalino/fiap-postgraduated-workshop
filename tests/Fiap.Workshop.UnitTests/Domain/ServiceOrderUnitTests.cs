using AutoFixture;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Domain.Errors;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class ServiceOrderUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "ServiceOrder >> Should be created >> When all required properties are provided")]
    public void ServiceOrder_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var problemDescription = _fixture.Create<string>();
        var odometerReading = _fixture.Create<int>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;
        var openedAt = DateTime.Now;
        var status = ServiceOrderStatus.InProgress;
        var diagnoseDescription = _fixture.Create<string>();
        var discount = _fixture.Create<decimal>();
        var subtotal = _fixture.Create<decimal>();
        var total = _fixture.Create<decimal>();
        var closedAt = DateTime.Now;

        // Act
        var serviceOrder = new ServiceOrder(
            id,
            customerId,
            vehicleId,
            createdBy,
            problemDescription,
            odometerReading,
            createdAt,
            updatedAt,
            openedAt,
            status,
            diagnoseDescription,
            discount,
            subtotal,
            total,
            closedAt
        );

        // Assert
        Assert.Equal(id, serviceOrder.Id);
        Assert.Equal(customerId, serviceOrder.CustomerId);
        Assert.Equal(vehicleId, serviceOrder.VehicleId);
        Assert.Equal(createdBy, serviceOrder.CreatedBy);
        Assert.Equal(problemDescription, serviceOrder.ProblemDescription);
        Assert.Equal(odometerReading, serviceOrder.OdometerReading);
        Assert.Equal(createdAt, serviceOrder.CreatedAt);
        Assert.Equal(updatedAt, serviceOrder.UpdatedAt);
        Assert.Equal(openedAt, serviceOrder.OpenedAt);
        Assert.Equal(status, serviceOrder.Status);
        Assert.Equal(diagnoseDescription, serviceOrder.DiagnoseDescription);
        Assert.Equal(discount, serviceOrder.Discount);
        Assert.Equal(subtotal, serviceOrder.Subtotal);
        Assert.Equal(total, serviceOrder.Total);
        Assert.Equal(closedAt, serviceOrder.ClosedAt);
    }

    [Fact(DisplayName = "ServiceOrder >> Should use default values >> When optional properties are not provided")]
    public void ServiceOrder_ShouldUseDefaultValues_WhenOptionalPropertiesAreNotProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var problemDescription = _fixture.Create<string>();
        var odometerReading = _fixture.Create<int>();
        var createdAt = DateTime.Now;
        var updatedAt = DateTime.Now;
        var openedAt = DateTime.Now;

        // Act
        var serviceOrder = new ServiceOrder(
            id,
            customerId,
            vehicleId,
            createdBy,
            problemDescription,
            odometerReading,
            createdAt,
            updatedAt,
            openedAt
        );

        // Assert
        Assert.Equal(ServiceOrderStatus.Received, serviceOrder.Status);
        Assert.Null(serviceOrder.DiagnoseDescription);
        Assert.Equal(default, serviceOrder.Discount);
        Assert.Equal(default, serviceOrder.Subtotal);
        Assert.Equal(default, serviceOrder.Total);
        Assert.Null(serviceOrder.ClosedAt);
        Assert.Empty(serviceOrder.Parts);
        Assert.Empty(serviceOrder.Services);
        Assert.Empty(serviceOrder.StatusHistory);
    }

    private static ServiceOrder CreateReceivedServiceOrder() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        "Engine noise",
        12000,
        DateTime.Now,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "ServiceOrder >> Should start diagnosis >> When status is Received")]
    public void ServiceOrder_ShouldStartDiagnosis_WhenStatusIsReceived()
    {
        // Arrange
        var serviceOrder = CreateReceivedServiceOrder();
        var diagnoseDescription = _fixture.Create<string>();
        var changedBy = Guid.NewGuid();
        var before = DateTime.Now;

        // Act
        serviceOrder.StartDiagnosis(diagnoseDescription, changedBy);

        // Assert
        Assert.Equal(ServiceOrderStatus.Diagnosing, serviceOrder.Status);
        Assert.Equal(diagnoseDescription, serviceOrder.DiagnoseDescription);
        Assert.InRange(serviceOrder.UpdatedAt, before, DateTime.Now);

        var history = Assert.Single(serviceOrder.StatusHistory);
        Assert.Equal(serviceOrder.Id, history.ServiceOrderId);
        Assert.Equal(ServiceOrderStatus.Received, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.Diagnosing, history.CurrentStatus);
        Assert.Equal(changedBy, history.ChangedBy);
        Assert.InRange(history.ChangedAt, before, DateTime.Now);
    }

    [Fact(DisplayName = "ServiceOrder >> Should throw >> When StartDiagnosis is called and status is not Received")]
    public void ServiceOrder_ShouldThrow_WhenStartDiagnosisIsCalledAndStatusIsNotReceived()
    {
        // Arrange
        var serviceOrder = CreateReceivedServiceOrder();
        serviceOrder.StartDiagnosis(_fixture.Create<string>(), Guid.NewGuid());

        // Act
        var act = () => serviceOrder.StartDiagnosis(_fixture.Create<string>(), Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(
            string.Format(ServiceOrderErrors.InvalidStatusTransition, ServiceOrderStatus.Diagnosing, ServiceOrderStatus.Diagnosing),
            exception.Message
        );
    }

    private static ServiceOrder CreateDiagnosingServiceOrder()
    {
        var serviceOrder = CreateReceivedServiceOrder();
        serviceOrder.StartDiagnosis("Worn brake pads", Guid.NewGuid());
        return serviceOrder;
    }

    private ServiceOrderPart CreatePart(Guid serviceOrderId, decimal unitPrice, int quantity) => new(
        Guid.NewGuid(),
        serviceOrderId,
        Guid.NewGuid(),
        _fixture.Create<string>(),
        _fixture.Create<string>(),
        unitPrice,
        quantity,
        DateTime.Now,
        DateTime.Now
    );

    private ServiceOrderService CreateService(Guid serviceOrderId, decimal unitPrice, int quantity) => new(
        Guid.NewGuid(),
        serviceOrderId,
        Guid.NewGuid(),
        _fixture.Create<string>(),
        _fixture.Create<string>(),
        unitPrice,
        30,
        quantity,
        DateTime.Now,
        DateTime.Now
    );

    [Fact(DisplayName = "ServiceOrder >> Should add budget >> When status is Diagnosing")]
    public void ServiceOrder_ShouldAddBudget_WhenStatusIsDiagnosing()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder();
        var part = CreatePart(serviceOrder.Id, 50m, 2);
        var service = CreateService(serviceOrder.Id, 120m, 1);
        var changedBy = Guid.NewGuid();
        var before = DateTime.Now;

        // Act
        serviceOrder.AddBudget([service], [part], changedBy);

        // Assert
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, serviceOrder.Status);
        Assert.Equal(220m, serviceOrder.Subtotal);
        Assert.Equal(0m, serviceOrder.Discount);
        Assert.Equal(220m, serviceOrder.Total);
        Assert.InRange(serviceOrder.UpdatedAt, before, DateTime.Now);

        Assert.Same(part, Assert.Single(serviceOrder.Parts));
        Assert.Same(service, Assert.Single(serviceOrder.Services));

        Assert.Equal(2, serviceOrder.StatusHistory.Count);
        var history = serviceOrder.StatusHistory.Last();
        Assert.Equal(ServiceOrderStatus.Diagnosing, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, history.CurrentStatus);
        Assert.Equal(changedBy, history.ChangedBy);
    }

    [Fact(DisplayName = "ServiceOrder >> Should throw >> When AddBudget is called and status is not Diagnosing")]
    public void ServiceOrder_ShouldThrow_WhenAddBudgetIsCalledAndStatusIsNotDiagnosing()
    {
        // Arrange
        var serviceOrder = CreateReceivedServiceOrder();

        // Act
        var act = () => serviceOrder.AddBudget([], [], Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(
            string.Format(ServiceOrderErrors.InvalidStatusTransition, ServiceOrderStatus.Received, ServiceOrderStatus.AwaitingApproval),
            exception.Message
        );
    }

    private static ServiceOrder CreateAwaitingApprovalServiceOrder()
    {
        var serviceOrder = CreateDiagnosingServiceOrder();
        serviceOrder.AddBudget([], [], Guid.NewGuid());
        return serviceOrder;
    }

    [Fact(DisplayName = "ServiceOrder >> Should approve >> When status is AwaitingApproval")]
    public void ServiceOrder_ShouldApprove_WhenStatusIsAwaitingApproval()
    {
        // Arrange
        var serviceOrder = CreateAwaitingApprovalServiceOrder();
        var changedBy = Guid.NewGuid();
        var before = DateTime.Now;

        // Act
        serviceOrder.Approve(changedBy);

        // Assert
        Assert.Equal(ServiceOrderStatus.InProgress, serviceOrder.Status);
        Assert.InRange(serviceOrder.UpdatedAt, before, DateTime.Now);

        Assert.Equal(3, serviceOrder.StatusHistory.Count);
        var history = serviceOrder.StatusHistory.Last();
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.InProgress, history.CurrentStatus);
        Assert.Equal(changedBy, history.ChangedBy);
    }

    [Fact(DisplayName = "ServiceOrder >> Should throw >> When Approve is called and status is not AwaitingApproval")]
    public void ServiceOrder_ShouldThrow_WhenApproveIsCalledAndStatusIsNotAwaitingApproval()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder();

        // Act
        var act = () => serviceOrder.Approve(Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(
            string.Format(ServiceOrderErrors.InvalidStatusTransition, ServiceOrderStatus.Diagnosing, ServiceOrderStatus.InProgress),
            exception.Message
        );
    }

    [Fact(DisplayName = "ServiceOrder >> Should reject >> When status is AwaitingApproval")]
    public void ServiceOrder_ShouldReject_WhenStatusIsAwaitingApproval()
    {
        // Arrange
        var serviceOrder = CreateAwaitingApprovalServiceOrder();
        var changedBy = Guid.NewGuid();
        var before = DateTime.Now;

        // Act
        serviceOrder.Reject(changedBy);

        // Assert
        Assert.Equal(ServiceOrderStatus.Cancelled, serviceOrder.Status);
        Assert.InRange(serviceOrder.UpdatedAt, before, DateTime.Now);

        Assert.Equal(3, serviceOrder.StatusHistory.Count);
        var history = serviceOrder.StatusHistory.Last();
        Assert.Equal(ServiceOrderStatus.AwaitingApproval, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.Cancelled, history.CurrentStatus);
        Assert.Equal(changedBy, history.ChangedBy);
    }

    [Fact(DisplayName = "ServiceOrder >> Should throw >> When Reject is called and status is not AwaitingApproval")]
    public void ServiceOrder_ShouldThrow_WhenRejectIsCalledAndStatusIsNotAwaitingApproval()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder();

        // Act
        var act = () => serviceOrder.Reject(Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(
            string.Format(ServiceOrderErrors.InvalidStatusTransition, ServiceOrderStatus.Diagnosing, ServiceOrderStatus.Cancelled),
            exception.Message
        );
    }

    private ServiceOrder CreateInProgressServiceOrder(out ServiceOrderService orderService)
    {
        var serviceOrder = CreateDiagnosingServiceOrder();
        orderService = CreateService(serviceOrder.Id, 120m, 1);

        serviceOrder.AddBudget([orderService], [], Guid.NewGuid());
        serviceOrder.Approve(Guid.NewGuid());

        return serviceOrder;
    }

    [Fact(DisplayName = "ServiceOrder >> Should complete >> When status is InProgress and every service has a duration")]
    public void ServiceOrder_ShouldComplete_WhenStatusIsInProgressAndEveryServiceHasADuration()
    {
        // Arrange
        var serviceOrder = CreateInProgressServiceOrder(out var orderService);
        var changedBy = Guid.NewGuid();
        var before = DateTime.Now;

        // Act
        serviceOrder.Complete([(orderService.Id, (short)45)], changedBy);

        // Assert
        Assert.Equal(ServiceOrderStatus.Completed, serviceOrder.Status);
        Assert.Equal((short)45, orderService.ActualDuration);
        Assert.InRange(serviceOrder.UpdatedAt, before, DateTime.Now);

        Assert.Equal(4, serviceOrder.StatusHistory.Count);
        var history = serviceOrder.StatusHistory.Last();
        Assert.Equal(ServiceOrderStatus.InProgress, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.Completed, history.CurrentStatus);
        Assert.Equal(changedBy, history.ChangedBy);
    }

    [Fact(DisplayName = "ServiceOrder >> Should throw >> When Complete is called and status is not InProgress")]
    public void ServiceOrder_ShouldThrow_WhenCompleteIsCalledAndStatusIsNotInProgress()
    {
        // Arrange
        var serviceOrder = CreateAwaitingApprovalServiceOrder();

        // Act
        var act = () => serviceOrder.Complete([], Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(
            string.Format(ServiceOrderErrors.InvalidStatusTransition, ServiceOrderStatus.AwaitingApproval, ServiceOrderStatus.Completed),
            exception.Message
        );
    }

    [Fact(DisplayName = "ServiceOrder >> Should throw >> When Complete is called and a service duration is missing")]
    public void ServiceOrder_ShouldThrow_WhenCompleteIsCalledAndAServiceDurationIsMissing()
    {
        // Arrange
        var serviceOrder = CreateInProgressServiceOrder(out _);

        // Act
        var act = () => serviceOrder.Complete([], Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceOrderErrors.MissingServiceDuration, exception.Message);
        Assert.Equal(ServiceOrderStatus.InProgress, serviceOrder.Status);
    }

    [Fact(DisplayName = "ServiceOrder >> Should throw >> When Complete is called with a duration for a service that does not belong to the order")]
    public void ServiceOrder_ShouldThrow_WhenCompleteIsCalledWithADurationForAServiceThatDoesNotBelongToTheOrder()
    {
        // Arrange
        var serviceOrder = CreateInProgressServiceOrder(out var orderService);
        var durations = new (Guid ServiceOrderServiceId, short ActualDuration)[]
        {
            (orderService.Id, (short)45),
            (Guid.NewGuid(), (short)30)
        };

        // Act
        var act = () => serviceOrder.Complete(durations, Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(ServiceOrderErrors.MissingServiceDuration, exception.Message);
        Assert.Equal(ServiceOrderStatus.InProgress, serviceOrder.Status);
    }

    private ServiceOrder CreateCompletedServiceOrder()
    {
        var serviceOrder = CreateInProgressServiceOrder(out var orderService);
        serviceOrder.Complete([(orderService.Id, (short)45)], Guid.NewGuid());
        return serviceOrder;
    }

    private static ServiceOrder CreateCancelledServiceOrder()
    {
        var serviceOrder = CreateAwaitingApprovalServiceOrder();
        serviceOrder.Reject(Guid.NewGuid());
        return serviceOrder;
    }

    [Fact(DisplayName = "ServiceOrder >> Should deliver >> When status is Completed")]
    public void ServiceOrder_ShouldDeliver_WhenStatusIsCompleted()
    {
        // Arrange
        var serviceOrder = CreateCompletedServiceOrder();
        var changedBy = Guid.NewGuid();
        var before = DateTime.Now;

        // Act
        serviceOrder.Deliver(changedBy);

        // Assert
        Assert.Equal(ServiceOrderStatus.Delivered, serviceOrder.Status);
        Assert.NotNull(serviceOrder.ClosedAt);
        Assert.InRange(serviceOrder.ClosedAt!.Value, before, DateTime.Now);
        Assert.InRange(serviceOrder.UpdatedAt, before, DateTime.Now);

        var history = serviceOrder.StatusHistory.Last();
        Assert.Equal(ServiceOrderStatus.Completed, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.Delivered, history.CurrentStatus);
        Assert.Equal(changedBy, history.ChangedBy);
    }

    [Fact(DisplayName = "ServiceOrder >> Should deliver >> When status is Cancelled")]
    public void ServiceOrder_ShouldDeliver_WhenStatusIsCancelled()
    {
        // Arrange — a rejected order still has the customer's vehicle at the shop until it is picked up.
        var serviceOrder = CreateCancelledServiceOrder();
        var changedBy = Guid.NewGuid();
        var before = DateTime.Now;

        // Act
        serviceOrder.Deliver(changedBy);

        // Assert
        Assert.Equal(ServiceOrderStatus.Delivered, serviceOrder.Status);
        Assert.NotNull(serviceOrder.ClosedAt);
        Assert.InRange(serviceOrder.ClosedAt!.Value, before, DateTime.Now);

        var history = serviceOrder.StatusHistory.Last();
        Assert.Equal(ServiceOrderStatus.Cancelled, history.PreviousStatus);
        Assert.Equal(ServiceOrderStatus.Delivered, history.CurrentStatus);
        Assert.Equal(changedBy, history.ChangedBy);
    }

    [Fact(DisplayName = "ServiceOrder >> Should throw >> When Deliver is called and status is neither Completed nor Cancelled")]
    public void ServiceOrder_ShouldThrow_WhenDeliverIsCalledAndStatusIsNeitherCompletedNorCancelled()
    {
        // Arrange
        var serviceOrder = CreateDiagnosingServiceOrder();

        // Act
        var act = () => serviceOrder.Deliver(Guid.NewGuid());

        // Assert
        var exception = Assert.Throws<DomainException>(act);
        Assert.Equal(
            string.Format(ServiceOrderErrors.InvalidStatusTransition, ServiceOrderStatus.Diagnosing, ServiceOrderStatus.Delivered),
            exception.Message
        );
        Assert.Null(serviceOrder.ClosedAt);
    }
}
