namespace HotelBooking.Modules.Rooms.Application.Amenities;
/// <summary>
/// DTO for creating or updating an amenity.
/// </summary>
/// <param name="Name"></param>
/// <param name="Description"></param>
public sealed record AmenityRequest(string Name, string? Description);
