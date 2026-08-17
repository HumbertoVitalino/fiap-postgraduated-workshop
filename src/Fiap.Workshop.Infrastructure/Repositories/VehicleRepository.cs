using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

internal sealed class VehicleRepository(AppDbContext context) : Repository<Vehicle>(context), IVehicleRepository
{
    public override async Task<Vehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

        return model?.MapToDomain();
    }

    public override async Task AddAsync(Vehicle entity, CancellationToken cancellationToken)
    {
        EnqueueEvents(entity);
        await _context.Vehicles.AddAsync(entity.MapToModel(), cancellationToken);
    }

    public override void Update(Vehicle entity)
    {
        EnqueueEvents(entity);
        Upsert(_context.Vehicles, entity.MapToModel(), entity.Id);
    }

    public override void Remove(Vehicle entity) =>
        Delete(_context.Vehicles, entity.MapToModel(), entity.Id);

    public async Task<bool> ExistsWithLicensePlateAsync(string licensePlate, CancellationToken cancellationToken)
    {
        return await _context.Vehicles
            .AsNoTracking()
            .AnyAsync(x => x.LicensePlate == licensePlate, cancellationToken);
    }

    public async Task<Vehicle?> GetByLicensePlateAsync(string licensePlate, CancellationToken cancellationToken)
    {
        var model = await _context.Vehicles
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.LicensePlate == licensePlate, cancellationToken);

        return model?.MapToDomain();
    }
}
