namespace HotelBooking.Shared.Exceptions;

/// <summary>The requested resource does not exist. Mapped to 404 Not Found.</summary>
public class NotFoundException(string message) : Exception(message)
{
    public NotFoundException(string resource, object key)
        : this($"{resource} with id '{key}' was not found.")
    {
    }
}
