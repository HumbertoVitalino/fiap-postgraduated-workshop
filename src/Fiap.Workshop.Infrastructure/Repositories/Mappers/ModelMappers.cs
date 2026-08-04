using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Models;

namespace Fiap.Workshop.Infrastructure.Repositories.Mappers;

internal static class ModelMappers
{
    internal static UserModel MapToModel(this User user)
    {
        return new(
            user.Id,
            user.Email,
            user.Name,
            user.Password,
            user.Role,
            user.CreatedAt,
            user.UpdatedAt
        );
    }

    internal static CustomerModel MapToModel(this Customer customer)
    {
        return new(
            customer.Id,
            customer.Name,
            customer.Document,
            customer.Email,
            customer.Phone,
            customer.CreatedAt,
            customer.UpdatedAt
        );
    }

    internal static VehicleModel MapToModel(this Vehicle vehicle)
    {
        return new(
            vehicle.Id,
            vehicle.CustomerId,
            vehicle.LicensePlate,
            vehicle.Brand,
            vehicle.Model,
            vehicle.ManufactureYear,
            vehicle.ModelYear,
            vehicle.Color,
            vehicle.CreatedAt,
            vehicle.UpdatedAt
        );
    }

    internal static InventoryItemModel MapToModel(this InventoryItem inventoryItem)
    {
        return new(
            inventoryItem.Id,
            inventoryItem.Code,
            inventoryItem.Name,
            inventoryItem.Description,
            inventoryItem.QuantityOnHand,
            inventoryItem.ReservedQuantity,
            inventoryItem.MinimumStock,
            inventoryItem.UnitPrice,
            inventoryItem.UnitOfMeasure,
            inventoryItem.IsActive,
            inventoryItem.CreatedAt,
            inventoryItem.UpdatedAt
        );
    }

    internal static ServiceModel MapToModel(this Service service)
    {
        return new(
            service.Id,
            service.Code,
            service.Name,
            service.Description,
            service.BasePrice,
            service.EstimatedDuration,
            service.IsActive,
            service.CreatedAt,
            service.UpdatedAt
        );
    }

    internal static ServiceOrderModel MapToModel(this ServiceOrder serviceOrder)
    {
        var model = new ServiceOrderModel(
            serviceOrder.Id,
            serviceOrder.Number,
            serviceOrder.CustomerId,
            serviceOrder.VehicleId,
            serviceOrder.CreatedBy,
            serviceOrder.Status,
            serviceOrder.ProblemDescription,
            serviceOrder.DiagnoseDescription,
            serviceOrder.OdometerReading,
            serviceOrder.Discount,
            serviceOrder.Subtotal,
            serviceOrder.Total,
            serviceOrder.OpenedAt,
            serviceOrder.ClosedAt,
            serviceOrder.CreatedAt,
            serviceOrder.UpdatedAt
        );

        model.Parts.AddRange(serviceOrder.Parts.Select(part => part.MapToModel()));
        model.Services.AddRange(serviceOrder.Services.Select(service => service.MapToModel()));
        model.StatusHistory.AddRange(serviceOrder.StatusHistory.Select(history => history.MapToModel()));

        return model;
    }

    internal static ServiceOrderPartModel MapToModel(this ServiceOrderPart part)
    {
        return new(
            part.Id,
            part.ServiceOrderId,
            part.InventoryItemId,
            part.Name,
            part.Description,
            part.UnitPrice,
            part.Quantity,
            part.CreatedAt,
            part.UpdatedAt
        );
    }

    internal static ServiceOrderServiceModel MapToModel(this ServiceOrderService service)
    {
        return new(
            service.Id,
            service.ServiceOrderId,
            service.ServiceId,
            service.Name,
            service.Description,
            service.UnitPrice,
            service.EstimatedDuration,
            service.Quantity,
            service.CreatedAt,
            service.UpdatedAt
        );
    }

    internal static ServiceOrderStatusHistoryModel MapToModel(this ServiceOrderStatusHistory statusHistory)
    {
        return new(
            statusHistory.Id,
            statusHistory.ServiceOrderId,
            statusHistory.PreviousStatus,
            statusHistory.CurrentStatus,
            statusHistory.ChangedBy,
            statusHistory.ChangedAt
        );
    }
}
