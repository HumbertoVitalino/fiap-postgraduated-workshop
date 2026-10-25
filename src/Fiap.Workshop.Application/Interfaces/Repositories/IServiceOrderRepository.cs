using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Domain.Enums;

namespace Fiap.Workshop.Application.Interfaces.Repositories;

public interface IServiceOrderRepository : IRepository<ServiceOrder>
{
    Task UpdateAsync(ServiceOrder entity, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ServiceOrder>> GetByStatusesAsync(IReadOnlyCollection<ServiceOrderStatus> statuses, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ServiceOrder>> GetAllByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task<bool> ExistsWithVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);
    Task<bool> ExistsWithServiceIdAsync(Guid serviceId, CancellationToken cancellationToken);
    Task<bool> ExistsWithInventoryItemIdAsync(Guid inventoryItemId, CancellationToken cancellationToken);
}
