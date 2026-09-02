namespace Bit.Butil;

/// <summary>
/// Wraps <see href="https://developer.mozilla.org/en-US/docs/Web/API/LaunchParams">LaunchParams</see>:
/// what an installed app was launched with.
/// </summary>
public class LaunchParams
{
    /// <summary>
    /// The URL the app was launched at. Empty for a plain launch from the app icon; set when the
    /// launch came from a protocol handler, a share target or a link capture.
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// The files the app was asked to open, in the order the OS handed them over. Empty for a launch
    /// that carried no files.
    /// </summary>
    public LaunchFile[] Files { get; set; } = [];
}
