using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

internal static class JsInteropConstants
{
    /// <summary>
    /// The set of <see cref="DynamicallyAccessedMemberTypes"/> required to preserve the JSON metadata
    /// of types that are serialized/deserialized across JS interop, so they survive trimming.
    /// </summary>
    public const DynamicallyAccessedMemberTypes JsonSerialized = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties;
}
