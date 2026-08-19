namespace Fiap.Workshop.Application.UseCases.Customers.DeleteCustomer.Boundaries;

public readonly struct DeleteCustomerInput(
    Guid correlationId,
    Guid customerId
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid CustomerId { get; init; } = customerId;
}
