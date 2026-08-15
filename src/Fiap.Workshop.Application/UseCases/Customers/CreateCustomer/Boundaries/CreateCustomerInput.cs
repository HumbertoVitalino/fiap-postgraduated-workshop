using Fiap.Workshop.Application.Commons;

namespace Fiap.Workshop.Application.UseCases.Customers.CreateCustomer.Boundaries;

public sealed record CreateCustomerInput
{
    public Guid CorrelationId { get; init; }
    public string Name { get; init; }
    public string Email { get; init; }
    public string Phone { get; init; }
    public string Document
    {
        get;
        init => field = value.StandardizeDocument();
    }

    public CreateCustomerInput(
        Guid correlationId,
        string name,
        string document,
        string email,
        string phone
    )
    {
        CorrelationId = correlationId;
        Name = name;
        Email = email;
        Phone = phone;
        Document = document;
    }
}