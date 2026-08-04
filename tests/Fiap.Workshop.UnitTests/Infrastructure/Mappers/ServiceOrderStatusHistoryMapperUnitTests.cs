using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class ServiceOrderStatusHistoryMapperUnitTests
{
    [Fact(DisplayName = "ServiceOrderStatusHistory >> Should map to model >> When mapping from domain")]
    public void ServiceOrderStatusHistory_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var statusHistory = new ServiceOrderStatusHistory(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ServiceOrderStatus.Received,
            ServiceOrderStatus.Diagnosing,
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        // Act
        var model = statusHistory.MapToModel();

        // Assert
        Assert.Equal(statusHistory.Id, model.Id);
        Assert.Equal(statusHistory.ServiceOrderId, model.ServiceOrderId);
        Assert.Equal(statusHistory.PreviousStatus, model.PreviousStatus);
        Assert.Equal(statusHistory.CurrentStatus, model.CurrentStatus);
        Assert.Equal(statusHistory.ChangedBy, model.ChangedBy);
        Assert.Equal(statusHistory.ChangedAt, model.ChangedAt);
    }

    [Fact(DisplayName = "ServiceOrderStatusHistoryModel >> Should map to domain >> When mapping from model")]
    public void ServiceOrderStatusHistoryModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = new ServiceOrderStatusHistoryModel(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ServiceOrderStatus.InProgress,
            ServiceOrderStatus.Completed,
            Guid.NewGuid(),
            DateTime.UtcNow
        );

        // Act
        var statusHistory = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, statusHistory.Id);
        Assert.Equal(model.ServiceOrderId, statusHistory.ServiceOrderId);
        Assert.Equal(model.PreviousStatus, statusHistory.PreviousStatus);
        Assert.Equal(model.CurrentStatus, statusHistory.CurrentStatus);
        Assert.Equal(model.ChangedBy, statusHistory.ChangedBy);
        Assert.Equal(model.ChangedAt, statusHistory.ChangedAt);
    }
}
