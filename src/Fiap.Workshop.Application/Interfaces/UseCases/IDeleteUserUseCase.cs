using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.DeleteUser.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IDeleteUserUseCase
{
    Task<Output> Handle(DeleteUserInput input, CancellationToken cancellationToken);
}
