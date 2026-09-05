namespace Bit.Butil;

/// <summary>
/// What a created WebNN context actually is - the answer to "which backend did I get".
/// </summary>
public class WebNNContextInfo
{
    /// <summary>
    /// The device the context runs on: <c>"cpu"</c>, <c>"gpu"</c> or <c>"npu"</c>. May differ from
    /// what was asked for - the request is a hint.
    /// </summary>
    public string DeviceType { get; set; } = string.Empty;

    /// <summary>The power preference in force: <c>"default"</c>, <c>"high-performance"</c> or <c>"low-power"</c>.</summary>
    public string PowerPreference { get; set; } = string.Empty;

    /// <summary>
    /// True when <c>MLGraphBuilder</c> exists, i.e. a model could actually be built and run on this
    /// context.
    /// </summary>
    public bool CanBuildGraph { get; set; }
}
