using Fiap.Workshop.Application.Commons;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetCustomersUseCase
{
    Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken);
}
