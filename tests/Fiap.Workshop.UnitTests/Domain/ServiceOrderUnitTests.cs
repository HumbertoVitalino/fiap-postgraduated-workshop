using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
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
        var number = _fixture.Create<int>();
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
            number,
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
        Assert.Equal(number, serviceOrder.Number);
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
        var number = _fixture.Create<int>();
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
            number,
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
}
