using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Services.DeleteService.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IDeleteServiceUseCase
{
    Task<Output> Handle(DeleteServiceInput input, CancellationToken cancellationToken);
}
