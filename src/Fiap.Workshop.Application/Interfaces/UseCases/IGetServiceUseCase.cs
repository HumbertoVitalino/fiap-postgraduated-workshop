using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Services.GetService.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetServiceUseCase
{
    Task<Output> Handle(GetServiceInput input, CancellationToken cancellationToken);
}
