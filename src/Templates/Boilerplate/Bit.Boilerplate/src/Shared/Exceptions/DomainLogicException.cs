namespace Boilerplate.Shared.Exceptions;

public partial class DomainLogicException : KnownException
{
    public DomainLogicException(string message)
        : base(message)
    {
    }

    public DomainLogicException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public DomainLogicException(LocalizedString message)
        : base(message)
    {
    }

    public DomainLogicException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
