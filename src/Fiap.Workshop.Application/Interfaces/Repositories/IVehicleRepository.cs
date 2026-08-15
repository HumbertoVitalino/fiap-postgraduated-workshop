using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.Interfaces.Repositories;

public interface IVehicleRepository : IRepository<Vehicle>
{
    Task<bool> ExistsWithLicensePlateAsync(string licensePlate, CancellationToken cancellationToken);
}
