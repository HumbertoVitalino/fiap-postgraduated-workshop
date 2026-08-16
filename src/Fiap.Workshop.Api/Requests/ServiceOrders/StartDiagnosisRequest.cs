namespace Fiap.Workshop.Api.Requests.ServiceOrders;

public sealed record StartDiagnosisRequest(
    Guid CorrelationId,
    string DiagnoseDescription
);
