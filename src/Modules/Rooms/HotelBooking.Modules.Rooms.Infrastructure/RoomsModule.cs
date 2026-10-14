using HotelBooking.Modules.Rooms.Application;
using HotelBooking.Modules.Rooms.Application.Amenities;
using HotelBooking.Modules.Rooms.Infrastructure.Persistence;
using HotelBooking.Modules.Rooms.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Modules.Rooms.Infrastructure;

public static class RoomsModule
{
    public static IServiceCollection AddRoomsModule(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddDbContext<RoomsDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
                sql.MigrationsHistoryTable(RoomsDbContext.MigrationsHistoryTable, RoomsDbContext.Schema)));

        services.AddScoped<IAmenityRepository, AmenityRepository>();

        services.AddRoomsApplication();

        return services;
    }
}
