namespace Fiap.Workshop.Application.DTOs.ServiceOrder;

public sealed record ServiceOrderTrackingResponse(
    Guid Id,
    string Status,
    string ProblemDescription,
    string? DiagnoseDescription,
    int OdometerReading,
    decimal Discount,
    decimal Subtotal,
    decimal Total,
    DateTime OpenedAt,
    DateTime? ClosedAt,
    IReadOnlyCollection<ServiceOrderPartResponse> Parts,
    IReadOnlyCollection<ServiceOrderServiceResponse> Services,
    IReadOnlyCollection<ServiceOrderTrackingStatusHistoryResponse> StatusHistory
);
