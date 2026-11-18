using System.Numerics;

namespace Bit.Bink;

/// <summary>
/// An element of a type this version of the library does not know. It is preserved verbatim through load/save so
/// documents created by newer versions or with custom element types are never silently damaged. It draws nothing.
/// </summary>
public sealed record BinkUnknownElement : BinkElement
{
    /// <summary>Creates an unknown element.</summary>
    /// <param name="typeName">The serialized type discriminator.</param>
    /// <param name="rawJson">The element's original JSON object.</param>
    /// <param name="bounds">Bounds recorded in the document, if any.</param>
    public BinkUnknownElement(string typeName, string rawJson, BinkRect bounds)
    {
        ArgumentException.ThrowIfNullOrEmpty(typeName);
        ArgumentNullException.ThrowIfNull(rawJson);
        SerializedTypeName = typeName;
        RawJson = rawJson;
        LocalBounds = bounds;
    }

    /// <summary>The type discriminator found in the document.</summary>
    public string SerializedTypeName { get; }

    /// <summary>The original JSON.</summary>
    public string RawJson { get; }

    /// <inheritdoc />
    public override string TypeName => "Unknown (" + SerializedTypeName + ")";

    /// <inheritdoc />
    public override bool IsSelectable => false;

    /// <inheritdoc />
    public override BinkRect LocalBounds { get; }

    /// <inheritdoc />
    protected override void DrawCore(IBinkRenderContext context) { }

    /// <inheritdoc />
    protected override bool HitTestCore(Vector2 localPoint, float tolerance) => false;
}
