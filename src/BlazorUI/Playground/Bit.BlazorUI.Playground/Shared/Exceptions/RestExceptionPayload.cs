using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Shared.Exceptions;

public class RestExceptionPayload
{
    public string? ExceptionType { get; set; }

    public string? Message { get; set; }

    public List<ResourceValidationExceptionPayload> Details { get; set; } = new();
}
