using HotelBooking.Modules.Rooms.Application.Amenities;
using HotelBooking.Modules.Rooms.Domain.Amenities;
using HotelBooking.Shared.Exceptions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Modules.Rooms.Infrastructure.Persistence.Repositories;

internal sealed class AmenityRepository(RoomsDbContext dbContext) : IAmenityRepository
{
    // SQL Server error numbers for a duplicate key in a unique index / unique constraint.
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    public async Task<IReadOnlyList<Amenity>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Amenities
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .ToListAsync(cancellationToken);

    public Task<Amenity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Amenities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, Guid? excludeId, CancellationToken cancellationToken) =>
        dbContext.Amenities.AnyAsync(
            a => a.Name == name && (excludeId == null || a.Id != excludeId),
            cancellationToken);

    public void Add(Amenity amenity) => dbContext.Amenities.Add(amenity);

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException
        {
            Number: UniqueIndexViolation or UniqueConstraintViolation
        })
        {
            // Another request saved the same name between our uniqueness check and this save.
            throw new ConflictException("An amenity with the same name already exists.");
        }
    }
}
