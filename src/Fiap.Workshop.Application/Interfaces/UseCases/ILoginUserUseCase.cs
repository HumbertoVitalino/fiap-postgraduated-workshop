using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.LoginUser.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ILoginUserUseCase
{
    Task<Output> Handle(LoginUserInput input, CancellationToken cancellationToken);
}