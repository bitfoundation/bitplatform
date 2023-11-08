﻿using System.Net;

namespace Bit.Websites.Sales.Shared.Exceptions;

public class ResourceNotFoundException : RestException
{
    public ResourceNotFoundException()
        : base(nameof(AppStrings.ResourceNotFoundException))
    {
    }

    public ResourceNotFoundException(string message)
        : base(message)
    {
    }

    public ResourceNotFoundException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public ResourceNotFoundException(LocalizedString message)
        : base(message)
    {
    }

    public ResourceNotFoundException(LocalizedString message, Exception? innerException)
        : base(message, innerException)
    {
    }

    public override HttpStatusCode StatusCode => HttpStatusCode.NotFound;
}
