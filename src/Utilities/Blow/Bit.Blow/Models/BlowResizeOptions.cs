namespace Bit.Blow;

/// <summary>
/// The size bounds of a node resize gesture, for <c>BlowCanvas.StartResize</c>.
/// </summary>
/// <remarks>
/// <para>
/// One argument rather than a parameter each: an interface method with more than eight parameters needs a bridge built
/// for its exact signature when an app compiled with profile-guided AOT reaches it on the interpreter, and none exists.
/// </para>
/// <para>
/// A value, so starting a gesture allocates nothing. <c>default</c> is a valid value: no lower bound past 1, no upper
/// bound, and the aspect ratio free.
/// </para>
/// </remarks>
public readonly record struct BlowResizeOptions
{
    /// <summary>The least width the node is resized to, in flow units; never below 1.</summary>
    public double MinWidth { get; init; }

    /// <summary>The least height the node is resized to, in flow units; never below 1.</summary>
    public double MinHeight { get; init; }

    /// <summary>
    /// The most width the node is resized to, in flow units; never below <see cref="MinWidth"/>. <c>null</c> is no bound.
    /// </summary>
    public double? MaxWidth { get; init; }

    /// <summary>
    /// The most height the node is resized to, in flow units; never below <see cref="MinHeight"/>. <c>null</c> is no bound.
    /// </summary>
    public double? MaxHeight { get; init; }

    /// <summary>Keeps the node's width-to-height ratio as it is when the gesture starts.</summary>
    public bool KeepAspectRatio { get; init; }
}
