using System;
using System.Runtime.Serialization;

namespace Bit.BlazorUI.Playground.Shared.Exceptions;

[Serializable]
public class DomainLogicException : KnownException
{
    public DomainLogicException(string? message)
        : base(message)
    {
    }

    public DomainLogicException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    protected DomainLogicException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
