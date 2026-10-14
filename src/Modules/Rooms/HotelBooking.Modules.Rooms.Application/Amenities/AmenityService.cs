using HotelBooking.Modules.Rooms.Domain.Amenities;
using HotelBooking.Shared.Exceptions;

namespace HotelBooking.Modules.Rooms.Application.Amenities;

public sealed class AmenityService(IAmenityRepository repository)
{
    public async Task<IReadOnlyList<AmenityResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var amenities = await repository.GetAllAsync(cancellationToken);
        return amenities.Select(AmenityResponse.FromEntity).ToList();
    }

    public async Task<AmenityResponse> CreateAsync(AmenityRequest request, CancellationToken cancellationToken = default)
    {
        var amenity = Amenity.Create(request.Name, request.Description);
        await EnsureNameIsUniqueAsync(amenity.Name, excludeId: null, cancellationToken);

        repository.Add(amenity);
        await repository.SaveChangesAsync(cancellationToken);

        return AmenityResponse.FromEntity(amenity);
    }

    public async Task<AmenityResponse> UpdateAsync(Guid id, AmenityRequest request, CancellationToken cancellationToken = default)
    {
        var amenity = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(Amenity), id);

        amenity.Update(request.Name, request.Description);
        await EnsureNameIsUniqueAsync(amenity.Name, amenity.Id, cancellationToken);

        await repository.SaveChangesAsync(cancellationToken);

        return AmenityResponse.FromEntity(amenity);
    }

    private async Task EnsureNameIsUniqueAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        if (await repository.NameExistsAsync(name, excludeId, cancellationToken))
        {
            throw new ConflictException($"An amenity named '{name}' already exists.");
        }
    }
}
