namespace Fiap.Workshop.Application.UseCases.ServiceOrders.StartDiagnosis.Boundaries;

public sealed record StartDiagnosisInput(
    Guid CorrelationId,
    Guid ServiceOrderId,
    Guid ChangedBy,
    string DiagnoseDescription
);
