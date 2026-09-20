using System.Diagnostics.CodeAnalysis;

namespace Bit.Butil;

/// <summary>
/// The <see cref="DynamicallyAccessedMemberTypes"/> combinations this library annotates with, named
/// once so a call site says what it is preserving rather than spelling out the flags again.
/// </summary>
public static class LinkerFlags
{
    /// <summary>
    /// Flags for a member that is JSON (de)serialized.
    /// </summary>
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;

    /// <summary>
    /// Flags for a component
    /// </summary>
    public const DynamicallyAccessedMemberTypes Component = DynamicallyAccessedMemberTypes.All;

    /// <summary>
    /// Flags for a JSInvokable type.
    /// </summary>
    public const DynamicallyAccessedMemberTypes JSInvokable = DynamicallyAccessedMemberTypes.PublicMethods;
}
