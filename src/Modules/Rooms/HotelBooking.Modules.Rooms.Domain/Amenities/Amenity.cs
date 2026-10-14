using HotelBooking.Shared.Domain;
using HotelBooking.Shared.Exceptions;

namespace HotelBooking.Modules.Rooms.Domain.Amenities;

public sealed class Amenity : Entity
{
    public const int NameMaxLength = 100;
    public const int DescriptionMaxLength = 500;

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    // Required by EF Core.
    private Amenity()
    {
    }

    public static Amenity Create(string name, string? description)
    {
        var amenity = new Amenity();
        amenity.SetDetails(name, description);
        return amenity;
    }

    public void Update(string name, string? description) => SetDetails(name, description);

    private void SetDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Amenity name is required.");
        }

        name = name.Trim();
        if (name.Length > NameMaxLength)
        {
            throw new DomainException($"Amenity name must be at most {NameMaxLength} characters.");
        }

        description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (description?.Length > DescriptionMaxLength)
        {
            throw new DomainException($"Amenity description must be at most {DescriptionMaxLength} characters.");
        }

        Name = name;
        Description = description;
    }
}
