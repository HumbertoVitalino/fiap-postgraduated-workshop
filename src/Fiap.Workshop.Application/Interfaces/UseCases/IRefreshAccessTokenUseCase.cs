using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Users.RefreshAccessToken.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IRefreshAccessTokenUseCase
{
    Task<Output> Handle(RefreshAccessTokenInput input, CancellationToken cancellationToken);
}
