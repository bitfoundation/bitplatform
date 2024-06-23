﻿namespace Bit.Websites.Platform.Shared.Exceptions;

public class ErrorResourcePayload
{
    public string? ResourceTypeName { get; set; } = "*";

    public List<PropertyErrorResourceCollection> Details { get; set; } = [];
}

public class PropertyErrorResourceCollection
{
    public string? Name { get; set; } = "*";

    public List<ErrorResource> Errors { get; set; } = [];
}

public class ErrorResource
{
    public string? Key { get; set; }

    public string? Message { get; set; }
}
