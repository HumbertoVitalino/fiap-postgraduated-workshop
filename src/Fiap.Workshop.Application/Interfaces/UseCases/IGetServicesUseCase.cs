using Fiap.Workshop.Application.Commons;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetServicesUseCase
{
    Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken);
}
