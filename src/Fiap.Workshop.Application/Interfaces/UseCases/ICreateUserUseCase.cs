using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Users.CreateUser.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ICreateUserUseCase
{
    Task<Output> ExecuteAsync(CreateUserInput input, CancellationToken cancellationToken = default);
}
