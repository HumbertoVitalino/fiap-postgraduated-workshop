using AutoFixture;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Xunit;

namespace Fiap.Workshop.UnitTests.Infrastructure.Mappers;

public class ServiceOrderMapperUnitTests
{
    private readonly Fixture _fixture = new();

    private ServiceOrder CreateServiceOrder() => new(
        Guid.NewGuid(),
        _fixture.Create<int>(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        _fixture.Create<string>(),
        _fixture.Create<int>(),
        DateTime.UtcNow,
        DateTime.UtcNow,
        DateTime.UtcNow,
        ServiceOrderStatus.InProgress,
        _fixture.Create<string>(),
        _fixture.Create<decimal>(),
        _fixture.Create<decimal>(),
        _fixture.Create<decimal>(),
        DateTime.UtcNow
    );

    [Fact(DisplayName = "ServiceOrder >> Should map to model >> When mapping from domain")]
    public void ServiceOrder_ShouldMapToModel_WhenMappingFromDomain()
    {
        // Arrange
        var serviceOrder = CreateServiceOrder();

        // Act
        var model = serviceOrder.MapToModel();

        // Assert
        Assert.Equal(serviceOrder.Id, model.Id);
        Assert.Equal(serviceOrder.Number, model.Number);
        Assert.Equal(serviceOrder.CustomerId, model.CustomerId);
        Assert.Equal(serviceOrder.VehicleId, model.VehicleId);
        Assert.Equal(serviceOrder.CreatedBy, model.CreatedBy);
        Assert.Equal(serviceOrder.Status, model.Status);
        Assert.Equal(serviceOrder.ProblemDescription, model.ProblemDescription);
        Assert.Equal(serviceOrder.DiagnoseDescription, model.DiagnoseDescription);
        Assert.Equal(serviceOrder.OdometerReading, model.OdometerReading);
        Assert.Equal(serviceOrder.Discount, model.Discount);
        Assert.Equal(serviceOrder.Subtotal, model.Subtotal);
        Assert.Equal(serviceOrder.Total, model.Total);
        Assert.Equal(serviceOrder.OpenedAt, model.OpenedAt);
        Assert.Equal(serviceOrder.ClosedAt, model.ClosedAt);
        Assert.Equal(serviceOrder.CreatedAt, model.CreatedAt);
        Assert.Equal(serviceOrder.UpdatedAt, model.UpdatedAt);
        Assert.Empty(model.Parts);
        Assert.Empty(model.Services);
        Assert.Empty(model.StatusHistory);
    }

    [Fact(DisplayName = "ServiceOrder >> Should map children to model >> When aggregate has parts, services and status history")]
    public void ServiceOrder_ShouldMapChildrenToModel_WhenAggregateHasPartsServicesAndStatusHistory()
    {
        // Arrange
        var serviceOrder = CreateServiceOrder();

        var part = new ServiceOrderPart(
            Guid.NewGuid(), serviceOrder.Id, Guid.NewGuid(),
            _fixture.Create<string>(), _fixture.Create<string>(),
            _fixture.Create<decimal>(), _fixture.Create<int>(),
            DateTime.UtcNow, DateTime.UtcNow);

        var service = new ServiceOrderService(
            Guid.NewGuid(), serviceOrder.Id, Guid.NewGuid(),
            _fixture.Create<string>(), _fixture.Create<string>(),
            _fixture.Create<decimal>(), _fixture.Create<short>(), _fixture.Create<int>(),
            DateTime.UtcNow, DateTime.UtcNow);

        var statusHistory = new ServiceOrderStatusHistory(
            Guid.NewGuid(), serviceOrder.Id,
            ServiceOrderStatus.Received, ServiceOrderStatus.Diagnosing,
            Guid.NewGuid(), DateTime.UtcNow);

        serviceOrder.AddParts([part]);
        serviceOrder.AddServices([service]);
        serviceOrder.AddStatusHistory([statusHistory]);

        // Act
        var model = serviceOrder.MapToModel();

        // Assert
        var mappedPart = Assert.Single(model.Parts);
        Assert.Equal(part.Id, mappedPart.Id);
        Assert.Equal(part.InventoryItemId, mappedPart.InventoryItemId);

        var mappedService = Assert.Single(model.Services);
        Assert.Equal(service.Id, mappedService.Id);
        Assert.Equal(service.ServiceId, mappedService.ServiceId);

        var mappedStatusHistory = Assert.Single(model.StatusHistory);
        Assert.Equal(statusHistory.Id, mappedStatusHistory.Id);
        Assert.Equal(statusHistory.CurrentStatus, mappedStatusHistory.CurrentStatus);
    }

    private static ServiceOrderModel CreateServiceOrderModel() => new(
        Guid.NewGuid(),
        1,
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        ServiceOrderStatus.AwaitingApproval,
        "problem",
        "diagnose",
        1000,
        10m,
        100m,
        90m,
        DateTime.UtcNow,
        DateTime.UtcNow,
        DateTime.UtcNow,
        DateTime.UtcNow
    );

    [Fact(DisplayName = "ServiceOrderModel >> Should map to domain >> When mapping from model")]
    public void ServiceOrderModel_ShouldMapToDomain_WhenMappingFromModel()
    {
        // Arrange
        var model = CreateServiceOrderModel();

        // Act
        var serviceOrder = model.MapToDomain();

        // Assert
        Assert.Equal(model.Id, serviceOrder.Id);
        Assert.Equal(model.Number, serviceOrder.Number);
        Assert.Equal(model.CustomerId, serviceOrder.CustomerId);
        Assert.Equal(model.VehicleId, serviceOrder.VehicleId);
        Assert.Equal(model.CreatedBy, serviceOrder.CreatedBy);
        Assert.Equal(model.Status, serviceOrder.Status);
        Assert.Equal(model.ProblemDescription, serviceOrder.ProblemDescription);
        Assert.Equal(model.DiagnoseDescription, serviceOrder.DiagnoseDescription);
        Assert.Equal(model.OdometerReading, serviceOrder.OdometerReading);
        Assert.Equal(model.Discount, serviceOrder.Discount);
        Assert.Equal(model.Subtotal, serviceOrder.Subtotal);
        Assert.Equal(model.Total, serviceOrder.Total);
        Assert.Equal(model.OpenedAt, serviceOrder.OpenedAt);
        Assert.Equal(model.ClosedAt, serviceOrder.ClosedAt);
        Assert.Equal(model.CreatedAt, serviceOrder.CreatedAt);
        Assert.Equal(model.UpdatedAt, serviceOrder.UpdatedAt);
        Assert.Empty(serviceOrder.Parts);
        Assert.Empty(serviceOrder.Services);
        Assert.Empty(serviceOrder.StatusHistory);
    }

    [Fact(DisplayName = "ServiceOrderModel >> Should map children to domain >> When model has parts, services and status history")]
    public void ServiceOrderModel_ShouldMapChildrenToDomain_WhenModelHasPartsServicesAndStatusHistory()
    {
        // Arrange
        var model = CreateServiceOrderModel();

        var partModel = new ServiceOrderPartModel(
            Guid.NewGuid(), model.Id, Guid.NewGuid(),
            _fixture.Create<string>(), _fixture.Create<string>(),
            _fixture.Create<decimal>(), _fixture.Create<int>(),
            DateTime.UtcNow, DateTime.UtcNow);

        var serviceModel = new ServiceOrderServiceModel(
            Guid.NewGuid(), model.Id, Guid.NewGuid(),
            _fixture.Create<string>(), _fixture.Create<string>(),
            _fixture.Create<decimal>(), _fixture.Create<short>(), _fixture.Create<int>(),
            DateTime.UtcNow, DateTime.UtcNow);

        var statusHistoryModel = new ServiceOrderStatusHistoryModel(
            Guid.NewGuid(), model.Id,
            ServiceOrderStatus.Diagnosing, ServiceOrderStatus.AwaitingApproval,
            Guid.NewGuid(), DateTime.UtcNow);

        model.Parts.Add(partModel);
        model.Services.Add(serviceModel);
        model.StatusHistory.Add(statusHistoryModel);

        // Act
        var serviceOrder = model.MapToDomain();

        // Assert
        var mappedPart = Assert.Single(serviceOrder.Parts);
        Assert.Equal(partModel.Id, mappedPart.Id);
        Assert.Equal(partModel.InventoryItemId, mappedPart.InventoryItemId);

        var mappedService = Assert.Single(serviceOrder.Services);
        Assert.Equal(serviceModel.Id, mappedService.Id);
        Assert.Equal(serviceModel.ServiceId, mappedService.ServiceId);

        var mappedStatusHistory = Assert.Single(serviceOrder.StatusHistory);
        Assert.Equal(statusHistoryModel.Id, mappedStatusHistory.Id);
        Assert.Equal(statusHistoryModel.CurrentStatus, mappedStatusHistory.CurrentStatus);
    }
}
