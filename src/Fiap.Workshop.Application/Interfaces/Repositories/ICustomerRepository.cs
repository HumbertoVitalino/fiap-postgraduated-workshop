using Fiap.Workshop.Application.Abstractions;
using Fiap.Workshop.Domain.Entities;

namespace Fiap.Workshop.Application.Interfaces.Repositories;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<bool> AnyAsync(string document, CancellationToken cancellationToken);
    Task<bool> ExistsWithEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> ExistsWithPhoneAsync(string phone, CancellationToken cancellationToken);
}
