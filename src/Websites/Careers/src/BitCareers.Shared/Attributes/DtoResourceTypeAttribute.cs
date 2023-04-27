﻿namespace BitCareers.Shared.Attributes;

/// <summary>
/// Gets or sets the resource type to use for error message & localizations lookups.
/// </summary>
public class DtoResourceTypeAttribute : Attribute
{
    public Type ResourceType { get; }

    public DtoResourceTypeAttribute(Type resourceType)
    {
        ResourceType = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
    }
}
