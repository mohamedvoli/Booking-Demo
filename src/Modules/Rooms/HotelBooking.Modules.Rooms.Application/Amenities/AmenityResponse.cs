using HotelBooking.Modules.Rooms.Domain.Amenities;

namespace HotelBooking.Modules.Rooms.Application.Amenities;
/// <summary>
/// DTO for returning amenity details in responses.
/// </summary>
/// <param name="Id"></param>
/// <param name="Name"></param>
/// <param name="Description"></param>
public sealed record AmenityResponse(Guid Id, string Name, string? Description)
{
    /// <summary>
    /// Mapping method to convert an Amenity entity to an AmenityResponse DTO.
    /// </summary>
    /// <param name="amenity"></param>
    /// <returns></returns>
    internal static AmenityResponse FromEntity(Amenity amenity) =>
        new(amenity.Id, amenity.Name, amenity.Description);
}
