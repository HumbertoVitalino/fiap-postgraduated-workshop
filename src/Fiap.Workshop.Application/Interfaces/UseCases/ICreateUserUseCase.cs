using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ICreateUserUseCase
{
    Task<Output> Handle(CreateUserInput input, CancellationToken cancellationToken);
}
