namespace Bit.Blow;

/// <summary>
/// The size bounds of a node resize gesture, for <c>BlowCanvas.StartResize</c>.
/// </summary>
/// <remarks>
/// One object rather than a parameter each: an interface method with more than eight parameters needs a bridge built
/// for its exact signature when an app compiled with profile-guided AOT reaches it on the interpreter, and none exists.
/// </remarks>
public sealed class BlowResizeOptions
{
    /// <summary>The least width the node is resized to, in flow units; never below 1.</summary>
    public double MinWidth { get; set; } = 1;

    /// <summary>The least height the node is resized to, in flow units; never below 1.</summary>
    public double MinHeight { get; set; } = 1;

    /// <summary>The most width the node is resized to, in flow units; never below <see cref="MinWidth"/>.</summary>
    public double MaxWidth { get; set; } = double.MaxValue;

    /// <summary>The most height the node is resized to, in flow units; never below <see cref="MinHeight"/>.</summary>
    public double MaxHeight { get; set; } = double.MaxValue;

    /// <summary>Keeps the node's width-to-height ratio as it is when the gesture starts.</summary>
    public bool KeepAspectRatio { get; set; }
}
