using HotelBooking.Modules.Rooms.Domain.Amenities;

namespace HotelBooking.Modules.Rooms.Application.Amenities;

public sealed record AmenityResponse(Guid Id, string Name, string? Description)
{
    internal static AmenityResponse FromEntity(Amenity amenity) =>
        new(amenity.Id, amenity.Name, amenity.Description);
}
