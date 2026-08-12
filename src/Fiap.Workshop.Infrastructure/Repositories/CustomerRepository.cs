using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

internal sealed class CustomerRepository(AppDbContext context) : Repository<Customer>(context), ICustomerRepository
{
    public override async Task<Customer?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return model?.MapToDomain();
    }

    public override async Task AddAsync(Customer entity, CancellationToken cancellationToken)
    {
        EnqueueEvents(entity);
        await _context.Customers.AddAsync(entity.MapToModel(), cancellationToken);
    }

    public override void Update(Customer entity)
    {
        EnqueueEvents(entity);
        Upsert(_context.Customers, entity.MapToModel(), entity.Id);
    }

    public override void Remove(Customer entity) =>
        Delete(_context.Customers, entity.MapToModel(), entity.Id);

    public async Task<bool> AnyAsync(string document, CancellationToken cancellationToken)
    {
        return await _context.Customers
            .AsNoTracking()
            .AnyAsync(x => x.Document == document, cancellationToken);
    }
}
