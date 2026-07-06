namespace Fiap.Workshop.Application.Interfaces;

public interface IUnitOfWork
{
    Task<bool> CommitAsync(CancellationToken cancellationToken = default);
}
