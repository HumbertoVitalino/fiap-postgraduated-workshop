using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Services.CreateService.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ICreateServiceUseCase
{
    Task<Output> Handle(CreateServiceInput input, CancellationToken cancellationToken);
}
