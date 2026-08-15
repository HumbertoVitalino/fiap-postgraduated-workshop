using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Models;

namespace Fiap.Workshop.Infrastructure.Repositories.Mappers;

internal static class DomainMappers
{
    internal static User MapToDomain(this UserModel model)
    {
        return new User(
            model.Id,
            model.Email,
            model.Name,
            model.Password,
            model.Role,
            model.CreatedAt,
            model.UpdatedAt
        );
    }

    internal static IEnumerable<User> MapToDomain(this IEnumerable<UserModel> model) => model.Select(MapToDomain);

    internal static Customer MapToDomain(this CustomerModel model)
    {
        return new Customer(
            model.Id,
            model.Name,
            model.Document,
            model.Email,
            model.Phone,
            model.CreatedAt,
            model.UpdatedAt
        );
    }

    internal static Vehicle MapToDomain(this VehicleModel model)
    {
        return new Vehicle(
            model.Id,
            model.CustomerId,
            model.LicensePlate,
            model.Brand,
            model.Model,
            model.ManufactureYear,
            model.ModelYear,
            model.Color,
            model.CreatedAt,
            model.UpdatedAt
        );
    }

    internal static InventoryItem MapToDomain(this InventoryItemModel model)
    {
        return new InventoryItem(
            model.Id,
            model.Code,
            model.Name,
            model.Description,
            model.QuantityOnHand,
            model.ReservedQuantity,
            model.MinimumStock,
            model.UnitPrice,
            model.UnitOfMeasure,
            model.IsActive,
            model.CreatedAt,
            model.UpdatedAt
        );
    }

    internal static IEnumerable<InventoryItem> MapToDomain(this IEnumerable<InventoryItemModel> model) => model.Select(MapToDomain);

    internal static Service MapToDomain(this ServiceModel model)
    {
        return new Service(
            model.Id,
            model.Code,
            model.Name,
            model.Description,
            model.BasePrice,
            model.EstimatedDuration,
            model.IsActive,
            model.CreatedAt,
            model.UpdatedAt
        );
    }

    internal static IEnumerable<Service> MapToDomain(this IEnumerable<ServiceModel> model) => model.Select(MapToDomain);

    internal static ServiceOrder MapToDomain(this ServiceOrderModel model)
    {
        var serviceOrder = new ServiceOrder(
            model.Id,
            model.Number,
            model.CustomerId,
            model.VehicleId,
            model.CreatedBy,
            model.ProblemDescription,
            model.OdometerReading,
            model.CreatedAt,
            model.UpdatedAt,
            model.OpenedAt,
            model.Status,
            model.DiagnoseDescription,
            model.Discount,
            model.Subtotal,
            model.Total,
            model.ClosedAt
        );

        serviceOrder.AddParts(model.Parts.Select(part => part.MapToDomain()));
        serviceOrder.AddServices(model.Services.Select(service => service.MapToDomain()));
        serviceOrder.AddStatusHistory(model.StatusHistory.Select(history => history.MapToDomain()));

        return serviceOrder;
    }

    internal static ServiceOrderPart MapToDomain(this ServiceOrderPartModel model)
    {
        return new ServiceOrderPart(
            model.Id,
            model.ServiceOrderId,
            model.InventoryItemId,
            model.Name,
            model.Description,
            model.UnitPrice,
            model.Quantity,
            model.CreatedAt,
            model.UpdatedAt
        );
    }

    internal static ServiceOrderService MapToDomain(this ServiceOrderServiceModel model)
    {
        return new ServiceOrderService(
            model.Id,
            model.ServiceOrderId,
            model.ServiceId,
            model.Name,
            model.Description,
            model.UnitPrice,
            model.EstimatedDuration,
            model.Quantity,
            model.CreatedAt,
            model.UpdatedAt
        );
    }

    internal static ServiceOrderStatusHistory MapToDomain(this ServiceOrderStatusHistoryModel model)
    {
        return new ServiceOrderStatusHistory(
            model.Id,
            model.ServiceOrderId,
            model.PreviousStatus,
            model.CurrentStatus,
            model.ChangedBy,
            model.ChangedAt
        );
    }
}
