using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Users.LogoutUser.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ILogoutUserUseCase
{
    Task<Output> Handle(LogoutUserInput input, CancellationToken cancellationToken);
}
