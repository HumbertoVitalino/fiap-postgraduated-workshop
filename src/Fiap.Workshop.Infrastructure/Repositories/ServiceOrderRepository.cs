using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

internal sealed class ServiceOrderRepository(AppDbContext context) : Repository<ServiceOrder>(context), IServiceOrderRepository
{
    public override async Task<ServiceOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await _context.ServiceOrders
            .AsNoTracking()
            .Include(so => so.Parts)
            .Include(so => so.Services)
            .Include(so => so.StatusHistory)
            .FirstOrDefaultAsync(so => so.Id == id, cancellationToken);

        return model?.MapToDomain();
    }

    public override async Task AddAsync(ServiceOrder entity, CancellationToken cancellationToken)
    {
        EnqueueEvents(entity);
        await _context.ServiceOrders.AddAsync(entity.MapToModel(), cancellationToken);
    }

    public override void Update(ServiceOrder entity)
    {
        EnqueueEvents(entity);
        Upsert(_context.ServiceOrders, entity.MapToModel(), entity.Id);
    }

    public async Task UpdateAsync(ServiceOrder entity, CancellationToken cancellationToken)
    {
        EnqueueEvents(entity);

        var model = entity.MapToModel();

        var trackedRoot = _context.ChangeTracker.Entries<ServiceOrderModel>()
            .FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (trackedRoot is not null)
            trackedRoot.CurrentValues.SetValues(model);
        else
            _context.Entry(model).State = EntityState.Modified;

        var existingPartIds = await _context.Set<ServiceOrderPartModel>()
            .Where(p => p.ServiceOrderId == entity.Id)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        ReconcileChildren(model.Parts, p => p.Id, existingPartIds);

        var existingServiceIds = await _context.Set<ServiceOrderServiceModel>()
            .Where(s => s.ServiceOrderId == entity.Id)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        ReconcileChildren(model.Services, s => s.Id, existingServiceIds);

        var existingHistoryIds = await _context.Set<ServiceOrderStatusHistoryModel>()
            .Where(h => h.ServiceOrderId == entity.Id)
            .Select(h => h.Id)
            .ToListAsync(cancellationToken);

        ReconcileChildren(model.StatusHistory, h => h.Id, existingHistoryIds);
    }

    private void ReconcileChildren<TModel>(List<TModel> incoming, Func<TModel, Guid> idSelector, IReadOnlyCollection<Guid> existingIds)
        where TModel : class
    {
        foreach (var item in incoming)
        {
            var id = idSelector(item);
            var trackedEntry = _context.ChangeTracker.Entries<TModel>().FirstOrDefault(e => idSelector(e.Entity) == id);

            if (trackedEntry is not null)
                trackedEntry.CurrentValues.SetValues(item);
            else
                _context.Entry(item).State = existingIds.Contains(id) ? EntityState.Modified : EntityState.Added;
        }
    }

    public override void Remove(ServiceOrder entity) =>
        Delete(_context.ServiceOrders, entity.MapToModel(), entity.Id);

    public async Task<IReadOnlyCollection<ServiceOrder>> GetAllByVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        var models = await _context.ServiceOrders
            .AsNoTracking()
            .Include(so => so.Parts)
            .Include(so => so.Services)
            .Include(so => so.StatusHistory)
            .Where(so => so.VehicleId == vehicleId)
            .ToListAsync(cancellationToken);

        return models.Select(model => model.MapToDomain()).ToList();
    }

    public async Task<bool> ExistsWithVehicleIdAsync(Guid vehicleId, CancellationToken cancellationToken)
    {
        return await _context.ServiceOrders
            .AsNoTracking()
            .AnyAsync(so => so.VehicleId == vehicleId, cancellationToken);
    }

    public async Task<bool> ExistsWithServiceIdAsync(Guid serviceId, CancellationToken cancellationToken)
    {
        return await _context.Set<ServiceOrderServiceModel>()
            .AsNoTracking()
            .AnyAsync(s => s.ServiceId == serviceId, cancellationToken);
    }
}
