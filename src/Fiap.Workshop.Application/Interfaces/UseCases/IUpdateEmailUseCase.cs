using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.Users.UpdateEmail.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IUpdateEmailUseCase
{
    Task<Output> ExecuteAsync(UpdateEmailInput input, CancellationToken cancellationToken = default);
}
