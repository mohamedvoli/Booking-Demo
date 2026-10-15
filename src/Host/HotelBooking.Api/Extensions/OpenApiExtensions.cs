namespace HotelBooking.Api.Extensions;

internal static class OpenApiExtensions
{
    public static IServiceCollection AddApiDocumentation(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
           
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Hotel Booking API";
                document.Info.Version = "v1";
                document.Info.Description = "API built with ASP.NET Core & Modular Monolith style and Clean Architecture pattern.";
                return Task.CompletedTask;
            });
        });

        return services;
    }
}
