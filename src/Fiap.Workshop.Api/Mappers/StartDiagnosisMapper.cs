using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.StartDiagnosis.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class StartDiagnosisMapper
{
    public static StartDiagnosisInput MapToInput(this StartDiagnosisRequest request, Guid serviceOrderId, Guid changedBy)
    {
        return new(
            request.CorrelationId,
            serviceOrderId,
            changedBy,
            request.DiagnoseDescription
        );
    }
}
