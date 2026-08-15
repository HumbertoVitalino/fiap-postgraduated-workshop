using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Users.LoginUser.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ILoginUserUseCase
{
    Task<Output> Handle(LoginUserInput input, CancellationToken cancellationToken);
}