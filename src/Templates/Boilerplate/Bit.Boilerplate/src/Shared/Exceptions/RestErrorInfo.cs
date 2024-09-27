﻿namespace Boilerplate.Shared.Exceptions;

public partial class RestErrorInfo
{
    public string? ExceptionType { get; set; }

    public string? Key { get; set; }

    public string? Message { get; set; }

    public ErrorResourcePayload? Payload { get; set; }
}
