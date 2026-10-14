namespace HotelBooking.Shared.Exceptions;

/// <summary>The request conflicts with the current state, such as a duplicate. Mapped to 409 Conflict.</summary>
public class ConflictException(string message) : Exception(message);
