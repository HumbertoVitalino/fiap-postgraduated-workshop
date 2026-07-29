using Fiap.Workshop.Domain.Abstractions;
using Fiap.Workshop.Application.Interfaces;

namespace Fiap.Workshop.Application.Abstractions;

public interface IRepository<T> where T : AggregateRoot
{
    IUnitOfWork UnitOfWork { get; }

    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task AddAsync(T entity, CancellationToken cancellationToken);
    void Update(T entity);
    void Remove(T entity);
}
