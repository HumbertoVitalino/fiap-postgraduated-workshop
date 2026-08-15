using Fiap.Workshop.Application.Commons;
using Fiap.Workshop.Application.UseCases.ServiceOrders.StartDiagnosis.Boundaries;

namespace Fiap.Workshop.Application.Interfaces.UseCases;

public interface IStartDiagnosisUseCase
{
    Task<Output> Handle(StartDiagnosisInput input, CancellationToken cancellationToken);
}
