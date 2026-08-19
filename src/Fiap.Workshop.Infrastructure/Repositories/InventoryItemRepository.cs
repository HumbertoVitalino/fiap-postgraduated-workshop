using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

internal sealed class InventoryItemRepository(AppDbContext context) : Repository<InventoryItem>(context), IInventoryItemRepository
{
    public override async Task<InventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await _context.InventoryItems
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return model?.MapToDomain();
    }

    public override async Task AddAsync(InventoryItem entity, CancellationToken cancellationToken)
    {
        EnqueueEvents(entity);
        await _context.InventoryItems.AddAsync(entity.MapToModel(), cancellationToken);
    }

    public override void Update(InventoryItem entity)
    {
        EnqueueEvents(entity);
        Upsert(_context.InventoryItems, entity.MapToModel(), entity.Id);
    }

    public override void Remove(InventoryItem entity) =>
        Delete(_context.InventoryItems, entity.MapToModel(), entity.Id);

    public async Task<bool> ExistsWithCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _context.InventoryItems
            .AsNoTracking()
            .AnyAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<InventoryItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        var inventoryItems = await _context.InventoryItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return inventoryItems.MapToDomain();
    }
}
