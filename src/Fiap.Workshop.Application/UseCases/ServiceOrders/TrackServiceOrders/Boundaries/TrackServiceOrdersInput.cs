using Fiap.Workshop.Application.Commons;

namespace Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders.Boundaries;

public sealed class TrackServiceOrdersInput
{
    public Guid CorrelationId { get; init; }
    public string Document
    {
        get;
        init => field = value.StandardizeDocument();
    }
    public string LicensePlate
    {
        get;
        init => field = value.NormalizeLicensePlate();
    }

    public TrackServiceOrdersInput(
        Guid correlationId,
        string document,
        string licensePlate
    )
    {
        CorrelationId = correlationId;
        Document = document;
        LicensePlate = licensePlate;
    }
}
