using HotelBooking.Modules.Rooms.Domain.Amenities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Modules.Rooms.Infrastructure.Persistence.Configurations;

internal sealed class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(Amenity.NameMaxLength);

        builder.Property(a => a.Description)
            .HasMaxLength(Amenity.DescriptionMaxLength);

        builder.HasIndex(a => a.Name).IsUnique();
    }
}
