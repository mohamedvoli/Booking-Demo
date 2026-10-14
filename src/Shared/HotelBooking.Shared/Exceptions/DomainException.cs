namespace HotelBooking.Shared.Exceptions;

/// <summary>A business rule was violated. Mapped to 400 Bad Request.</summary>
public class DomainException(string message) : Exception(message);
