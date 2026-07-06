using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Users.Login.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface ILoginUseCase
{
    Task<Output> ExecuteAsync(LoginInput input, CancellationToken cancellationToken = default);
}
