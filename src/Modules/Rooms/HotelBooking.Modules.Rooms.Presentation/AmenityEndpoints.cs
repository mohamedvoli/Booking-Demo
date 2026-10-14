using HotelBooking.Modules.Rooms.Application.Amenities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace HotelBooking.Modules.Rooms.Presentation;

public static class AmenityEndpoints
{
    public static IEndpointRouteBuilder MapAmenityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/amenities").WithTags("Amenities");

        group.MapGet("/", GetAllAsync).WithName("GetAmenities");
        group.MapPost("/", CreateAsync).WithName("CreateAmenity");
        group.MapPut("/{id:guid}", UpdateAsync).WithName("UpdateAmenity");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<AmenityResponse>>> GetAllAsync(
        AmenityService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.GetAllAsync(cancellationToken));

    private static async Task<Created<AmenityResponse>> CreateAsync(
        AmenityRequest request,
        AmenityService service,
        CancellationToken cancellationToken)
    {
        var amenity = await service.CreateAsync(request, cancellationToken);

        // No Location header: there is no GET /api/amenities/{id} endpoint to point to.
        return TypedResults.Created((string?)null, amenity);
    }

    private static async Task<Ok<AmenityResponse>> UpdateAsync(
        Guid id,
        AmenityRequest request,
        AmenityService service,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await service.UpdateAsync(id, request, cancellationToken));
}
