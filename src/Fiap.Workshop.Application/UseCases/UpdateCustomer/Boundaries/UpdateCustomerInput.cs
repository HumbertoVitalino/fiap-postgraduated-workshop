namespace Fiap.Workshop.Application.UseCases.UpdateCustomer.Boundaries;

public sealed class UpdateCustomerInput(
    Guid correlationId,
    Guid customerId,
    string name,
    string email,
    string phone
)
{
    public Guid CorrelationId { get; init; } = correlationId;
    public Guid CustomerId { get; init; } = customerId;
    public string Name { get; init; } = name;
    public string Email { get; init; } = email;
    public string Phone { get; init; } = phone;
}
