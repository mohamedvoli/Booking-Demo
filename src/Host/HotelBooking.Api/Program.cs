using HotelBooking.Api.ErrorHandling;
using HotelBooking.Api.Extensions;
using HotelBooking.Modules.Rooms.Infrastructure;
using HotelBooking.Modules.Rooms.Presentation;
using Scalar.AspNetCore;

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
