using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Services.UpdateService.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IUpdateServiceUseCase
{
    Task<Output> Handle(UpdateServiceInput input, CancellationToken cancellationToken);
}
