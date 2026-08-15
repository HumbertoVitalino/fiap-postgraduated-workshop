namespace Fiap.Workshop.Application.UseCases.ServiceOrders.CreateServiceOrder.Boundaries;

public sealed record CreateServiceOrderInput(
    Guid CorrelationId,
    Guid CustomerId,
    Guid VehicleId,
    Guid CreatedBy,
    string ProblemDescription,
    int OdometerReading
);
