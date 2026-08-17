using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.Interfaces.Repositories;

public interface IServiceOrderRepository : IRepository<ServiceOrder>
{
    Task UpdateAsync(ServiceOrder entity, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ServiceOrder>> GetAllByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken);
}
