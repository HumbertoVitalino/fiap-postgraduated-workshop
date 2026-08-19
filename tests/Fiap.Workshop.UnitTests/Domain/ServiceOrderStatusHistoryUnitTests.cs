using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Xunit;

namespace Fiap.Workshop.UnitTests.Domain;

public class ServiceOrderStatusHistoryUnitTests
{
    private readonly Fixture _fixture = new();

    [Fact(DisplayName = "ServiceOrderStatusHistory >> Should be created >> When all required properties are provided")]
    public void ServiceOrderStatusHistory_ShouldBeCreated_WhenAllRequiredPropertiesAreProvided()
    {
        // Arrange
        var id = Guid.NewGuid();
        var serviceOrderId = Guid.NewGuid();
        var previousStatus = ServiceOrderStatus.Diagnosing;
        var currentStatus = ServiceOrderStatus.AwaitingApproval;
        var changedBy = Guid.NewGuid();
        var changedAt = DateTime.Now;

        // Act
        var statusHistory = new ServiceOrderStatusHistory(
            id,
            serviceOrderId,
            previousStatus,
            currentStatus,
            changedBy,
            changedAt
        );

        // Assert
        Assert.Equal(id, statusHistory.Id);
        Assert.Equal(serviceOrderId, statusHistory.ServiceOrderId);
        Assert.Equal(previousStatus, statusHistory.PreviousStatus);
        Assert.Equal(currentStatus, statusHistory.CurrentStatus);
        Assert.Equal(changedBy, statusHistory.ChangedBy);
        Assert.Equal(changedAt, statusHistory.ChangedAt);
    }
}
