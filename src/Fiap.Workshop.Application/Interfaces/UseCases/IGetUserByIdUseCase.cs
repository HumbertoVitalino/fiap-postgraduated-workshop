using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Users.GetUserById.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IGetUserByIdUseCase
{
    Task<Output> ExecuteAsync(GetUserByIdInput input, CancellationToken cancellationToken = default);
}
