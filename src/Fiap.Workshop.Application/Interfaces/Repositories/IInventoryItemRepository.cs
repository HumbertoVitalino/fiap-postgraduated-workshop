using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.Interfaces.Repositories;

public interface IInventoryItemRepository : IRepository<InventoryItem>
{
    Task<bool> ExistsWithCodeAsync(string code, CancellationToken cancellationToken);
}
