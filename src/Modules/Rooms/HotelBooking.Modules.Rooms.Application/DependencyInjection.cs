using HotelBooking.Modules.Rooms.Application.Amenities;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Modules.Rooms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddRoomsApplication(this IServiceCollection services)
    {
        services.AddScoped<AmenityService>();

        return services;
    }
}
