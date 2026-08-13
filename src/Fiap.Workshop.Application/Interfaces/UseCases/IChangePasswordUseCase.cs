using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ChangePassword.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IChangePasswordUseCase
{
    Task<Output> Handle(ChangePasswordInput input, CancellationToken cancellationToken);
}
