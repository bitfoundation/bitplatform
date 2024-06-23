namespace Bit.BlazorUI.Demo.Shared.Exceptions;

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

    public KnownException(LocalizedString message)
        : base(message.Value)
    {
        Key = message.Name;
    }

    public KnownException(LocalizedString message, Exception? innerException)
        : base(message.Value, innerException)
    {
        Key = message.Name;
    }

    public string? Key { get; set; }
}
