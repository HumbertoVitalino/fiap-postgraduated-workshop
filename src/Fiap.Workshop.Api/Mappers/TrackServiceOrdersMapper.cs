using Fiap.Workshop.Api.Requests.ServiceOrders;
using Fiap.Workshop.Application.UseCases.ServiceOrders.TrackServiceOrders.Boundaries;

namespace Fiap.Workshop.Api.Mappers;

public static class TrackServiceOrdersMapper
{
    public static TrackServiceOrdersInput MapToInput(this TrackServiceOrdersRequest request)
    {
        return new(
            Guid.NewGuid(),
            request.Document,
            request.LicensePlate
        );
    }
}
