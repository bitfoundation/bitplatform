namespace Bit.Butil;

/// <summary>
/// What one operator supports on a given backend - an entry of
/// <see cref="WebNNContext.GetOpSupportLimits"/>.
/// </summary>
/// <remarks>
/// The detail differs per operator and per backend, and has changed shape between releases, so it is
/// reported as JSON rather than modelled: the useful question here is which operators exist at all,
/// which is answered by <see cref="Name"/>.
/// </remarks>
public class WebNNOpSupport
{
    /// <summary>The operator's name - <c>"conv2d"</c>, <c>"matmul"</c>, <c>"softmax"</c>…</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The backend's limits for it, as JSON - data types, rank limits, layouts.</summary>
    public string Detail { get; set; } = string.Empty;
}
