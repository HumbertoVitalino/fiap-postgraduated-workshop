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
}
