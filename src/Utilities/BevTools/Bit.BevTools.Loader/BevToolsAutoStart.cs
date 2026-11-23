namespace Bit.BevTools;

/// <summary>
/// The parts of BevTools that start with the app when BevTools itself waits to be attached
/// (<see cref="BevToolsLoaderOptions.AutoStart"/>). Each part runs on its own, inside Bit.BevTools.Loader, and none of
/// them starts the rest of BevTools: no component is instrumented, no render measured, nothing loaded or downloaded.
/// What a part collects is handed to the session <see cref="BevToolsLoader.OpenAsync()"/> attaches later, so that session
/// also shows what happened before it existed.
/// </summary>
[Flags]
public enum BevToolsAutoStart
{
    /// <summary>Nothing starts before the attach; an attached session sees what happens after it (default).</summary>
    None = 0,

    /// <summary>
    /// What the app's <c>ILogger</c>s write, from the moment the app's logger factory is built: level, category, event
    /// id, message, template values, scopes, exception and trace id, at <see cref="BevToolsLoaderOptions.MinimumLogLevel"/>
    /// or above, bounded by <see cref="BevToolsLoaderOptions.MaxLogs"/>. The attached session shows them in its Logs panel.
    /// </summary>
    Logs = 1,
}
