using HotelBooking.Modules.Rooms.Domain.Amenities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Modules.Rooms.Infrastructure.Persistence;

public sealed class RoomsDbContext(DbContextOptions<RoomsDbContext> options) : DbContext(options)
{
    public const string Schema = "dbo";
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Rooms";

    public DbSet<Amenity> Amenities => Set<Amenity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RoomsDbContext).Assembly);
    }
}
