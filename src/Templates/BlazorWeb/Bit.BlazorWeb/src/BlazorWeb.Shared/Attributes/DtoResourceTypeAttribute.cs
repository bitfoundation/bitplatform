namespace BlazorWeb.Shared.Attributes;

/// <summary>
/// Gets or sets the resource type to use for error message and localizations lookups.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DtoResourceTypeAttribute(Type resourceType) : Attribute
{
    public Type ResourceType { get; } = resourceType ?? throw new ArgumentNullException(nameof(resourceType));
}
