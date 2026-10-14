using HotelBooking.Modules.Rooms.Domain.Amenities;

namespace HotelBooking.Modules.Rooms.Application.Amenities;

public interface IAmenityRepository
{
    Task<IReadOnlyList<Amenity>> GetAllAsync(CancellationToken cancellationToken);

    Task<Amenity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Checks whether another amenity already uses <paramref name="name"/>.</summary>
    /// <param name="excludeId">The amenity being updated, which may keep its own name.</param>
    Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    void Add(Amenity amenity);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
