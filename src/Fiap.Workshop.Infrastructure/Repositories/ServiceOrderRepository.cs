using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
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

    public override void Remove(ServiceOrder entity) =>
        Delete(_context.ServiceOrders, entity.MapToModel(), entity.Id);
}
