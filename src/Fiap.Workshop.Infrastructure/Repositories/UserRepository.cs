using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Entities;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

internal sealed class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var model = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return model?.MapToDomain();
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var model = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        return model?.MapToDomain();
    }

    public async Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken) =>
        await _context.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public override async Task AddAsync(User entity, CancellationToken cancellationToken)
    {
        EnqueueEvents(entity);
        await _context.Users.AddAsync(entity.MapToModel(), cancellationToken);
    }

    public override void Update(User entity)
    {
        EnqueueEvents(entity);
        Upsert(_context.Users, entity.MapToModel(), entity.Id);
    }

    public override void Remove(User entity) =>
        Delete(_context.Users, entity.MapToModel(), entity.Id);
}
