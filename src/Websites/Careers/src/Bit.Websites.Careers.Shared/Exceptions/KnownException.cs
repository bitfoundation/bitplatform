namespace Bit.Websites.Careers.Shared.Exceptions;

public abstract class KnownException : Exception
{
    public KnownException(string message)
        : base(message)
    {
        Key = message;
    }

    public KnownException(string message, Exception? innerException)
        : base(message, innerException)
    {
        Key = message;
    }

    public string? Key { get; set; }
}
