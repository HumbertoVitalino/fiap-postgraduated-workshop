using Fiap.Workshop.Application.Interfaces;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Users;
using Fiap.Workshop.Infrastructure.Repositories.Mappers;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Microsoft.EntityFrameworkCore;

namespace Fiap.Workshop.Infrastructure.Repositories;

internal sealed class UserRepository(AppDbContext context) : IUserRepository
{
    public IUnitOfWork UnitOfWork => context;

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var model = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        return model?.MapToDomain();
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        var model = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email.Value, cancellationToken);

        return model?.MapToDomain();
    }

    public async Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        await context.Users.AnyAsync(u => u.Email == email.Value, cancellationToken);

    public async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        context.EnqueueDomainEvents(entity.GetDomainEvents());
        entity.ClearDomainEvents();
        await context.Users.AddAsync(entity.MapToModel(), cancellationToken);
    }

    public void Update(User entity)
    {
        context.EnqueueDomainEvents(entity.GetDomainEvents());
        entity.ClearDomainEvents();

        var model = entity.MapToModel();
        var tracked = context.ChangeTracker.Entries<UserModel>()
            .FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (tracked is not null)
            tracked.CurrentValues.SetValues(model);
        else
            context.Users.Update(model);
    }

    public void Remove(User entity)
    {
        var tracked = context.ChangeTracker.Entries<UserModel>()
            .FirstOrDefault(e => e.Entity.Id == entity.Id);

        if (tracked is not null)
            tracked.State = EntityState.Deleted;
        else
            context.Users.Remove(entity.MapToModel());
    }
}
