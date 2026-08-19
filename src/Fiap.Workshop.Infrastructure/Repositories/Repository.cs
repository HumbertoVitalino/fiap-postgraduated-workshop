using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Application.Interfaces.Repositories;
using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Infrastructure.Repositories.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace Fiap.Workshop.Infrastructure.Repositories;

[ExcludeFromCodeCoverage]
internal abstract class Repository<T>(AppDbContext context) : IRepository<T> where T : AggregateRoot
{
    protected readonly AppDbContext _context = context;

    public IUnitOfWork UnitOfWork => _context;

    public abstract Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    public abstract Task AddAsync(T entity, CancellationToken cancellationToken);

    public abstract void Update(T entity);

    public abstract void Remove(T entity);

    protected void EnqueueEvents(T entity)
    {
        _context.EnqueueDomainEvents(entity.GetDomainEvents());
        entity.ClearDomainEvents();
    }

    protected void Upsert<TModel>(DbSet<TModel> set, TModel model, Guid id)
        where TModel : Model
    {
        var tracked = _context.ChangeTracker.Entries<TModel>()
            .FirstOrDefault(e => e.Entity.Id == id);

        if (tracked is not null)
            tracked.CurrentValues.SetValues(model);
        else
            set.Update(model);
    }

    protected void Delete<TModel>(DbSet<TModel> set, TModel model, Guid id)
        where TModel : Model
    {
        var tracked = _context.ChangeTracker.Entries<TModel>()
            .FirstOrDefault(e => e.Entity.Id == id);

        if (tracked is not null)
            tracked.State = EntityState.Deleted;
        else
            set.Remove(model);
    }
}
