namespace Boilerplate.Shared.Exceptions;

public partial class ClientNotSupportedException : BadRequestException
{
    public ClientNotSupportedException()
        : this(nameof(AppStrings.ForceUpdateTitle))
    {
    }

    public ClientNotSupportedException(string message)
        : base(message)
    {
    }

    public ClientNotSupportedException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public ClientNotSupportedException(LocalizedString message)
        : base(message)
    {
    }

    public ClientNotSupportedException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
