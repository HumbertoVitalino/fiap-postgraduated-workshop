using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.UpdateUser.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IUpdateUserUseCase
{
    Task<Output> Handle(UpdateUserInput input, CancellationToken cancellationToken);
}
