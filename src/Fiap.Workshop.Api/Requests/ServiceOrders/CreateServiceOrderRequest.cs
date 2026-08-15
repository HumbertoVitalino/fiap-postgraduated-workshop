namespace Fiap.Workshop.Api.Requests.ServiceOrders;

public sealed record CreateServiceOrderRequest(
    Guid CorrelationId,
    Guid CustomerId,
    Guid VehicleId,
    string ProblemDescription,
    int OdometerReading
);
