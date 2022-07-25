using System;
using System.Collections.Generic;

namespace Bit.BlazorUI.Playground.Shared.Exceptions;

public class ResourceValidationExceptionPayload
{
    public string? ResourceTypeName { get; set; } = "*";

    public string? Property { get; set; } = "*";

    public IEnumerable<string> Messages { get; set; } = Array.Empty<string>();
}
