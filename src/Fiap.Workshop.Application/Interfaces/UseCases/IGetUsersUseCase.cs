using Fiap.Workshop.Application.Commons;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetUsersUseCase
{
    Task<Output> Handle(Guid correlationId, CancellationToken cancellationToken);
}
