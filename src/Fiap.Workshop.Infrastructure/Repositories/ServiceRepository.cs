using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

internal sealed class ServiceRepository(AppDbContext context) : Repository<Service>(context), IServiceRepository
{
    public override async Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await _context.Services
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return model?.MapToDomain();
    }

    public override async Task AddAsync(Service entity, CancellationToken cancellationToken)
    {
        EnqueueEvents(entity);
        await _context.Services.AddAsync(entity.MapToModel(), cancellationToken);
    }

    public override void Update(Service entity)
    {
        EnqueueEvents(entity);
        Upsert(_context.Services, entity.MapToModel(), entity.Id);
    }

    public override void Remove(Service entity) =>
        Delete(_context.Services, entity.MapToModel(), entity.Id);

    public async Task<bool> ExistsWithCodeAsync(string code, CancellationToken cancellationToken)
    {
        return await _context.Services
            .AsNoTracking()
            .AnyAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<IEnumerable<Service>> GetAllAsync(CancellationToken cancellationToken)
    {
        var services = await _context.Services
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return services.MapToDomain();
    }
}
