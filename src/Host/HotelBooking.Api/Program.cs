using HotelBooking.Api.ErrorHandling;
using HotelBooking.Api.Extensions;
using HotelBooking.Modules.Rooms.Infrastructure;
using HotelBooking.Modules.Rooms.Presentation;
using Scalar.AspNetCore;
using Serilog;


// 1. Initial "Bootstrap" logger to catch host initialization crashes
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
    {
        Log.Information("Starting Hotel Booking API host...");
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApiDocumentation();
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddRoomsModule(builder.Configuration);

        var app = builder.Build();

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.MapAmenityEndpoints();

        app.Run();
    }
catch (Exception ex) when (ex is not HostAbortedException) // Ignore EF migration aborts
    {
        Log.Fatal(ex, "Application terminated unexpectedly during startup.");
    }
finally
    {
        // 4. Ensure all buffered log events are flushed to disk before closing
        Log.CloseAndFlush();
    }