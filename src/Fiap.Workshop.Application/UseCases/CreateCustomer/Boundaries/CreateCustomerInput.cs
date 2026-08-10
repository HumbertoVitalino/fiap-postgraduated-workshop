namespace Fiap.Workshop.Application.UseCases.CreateCustomer.Boundaries;

public sealed record CreateCustomerInput(
    Guid CorrelationId,
    string Name,
    string Document,
    string Email,
    string Phone
);