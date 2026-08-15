namespace Fiap.Workshop.Application.DTOs.ServiceOrder;

public sealed record ServiceOrderResponse(
    Guid Id,
    Guid CustomerId,
    Guid VehicleId,
    Guid CreatedBy,
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
    IReadOnlyCollection<ServiceOrderStatusHistoryResponse> StatusHistory
);
