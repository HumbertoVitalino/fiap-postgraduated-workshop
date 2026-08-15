namespace Fiap.Workshop.Application.UseCases.GetCustomer.Boundaries;

public readonly struct GetCustomerInput(
    Guid correlationId,
    Guid customerId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid CustomerId { get; init; } = customerId;
}
