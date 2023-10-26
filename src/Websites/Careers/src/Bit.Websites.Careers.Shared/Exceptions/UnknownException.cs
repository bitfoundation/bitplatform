namespace Bit.Websites.Careers.Shared.Exceptions;

public class UnknownException : Exception
{
    public UnknownException(string message)
        : base(message)
    {
    }

    public UnknownException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

