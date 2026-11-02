using Microsoft.Extensions.Logging;

namespace Bit.Balette;

/// <summary>The palette's log messages, written through <see cref="ILogger"/> so they reach the app's own logging.</summary>
internal static partial class BaletteLog
{
    [LoggerMessage(1, LogLevel.Error, "The command '{CommandId}' ({Title}) failed.")]
    public static partial void CommandFailed(ILogger logger, string commandId, string title, Exception exception);

    [LoggerMessage(2, LogLevel.Error, "Loading the page of the command '{CommandId}' ({Title}) failed.")]
    public static partial void ChildrenFailed(ILogger logger, string commandId, string title, Exception exception);

    [LoggerMessage(3, LogLevel.Warning, "The async source '{Source}' failed; the palette offers a retry instead of its results.")]
    public static partial void SourceFailed(ILogger logger, string source, Exception exception);

    [LoggerMessage(11, LogLevel.Warning, "The async source '{Source}' did not answer within {TimeoutMs} ms; the palette offers a retry instead of its results.")]
    public static partial void SourceTimedOut(ILogger logger, string source, int timeoutMs);

    [LoggerMessage(4, LogLevel.Warning, "The command '{CommandId}' ({Title}) was not run: its link uses the scheme '{Scheme}', which BaletteOptions.AllowedUrlSchemes does not allow.")]
    public static partial void UnsafeLink(ILogger logger, string commandId, string title, string scheme);

    [LoggerMessage(5, LogLevel.Warning, "Two commands registered together have the id '{CommandId}' ('{FirstTitle}' and '{SecondTitle}'); the second replaces the first. Give them explicit ids with BaletteCommandBuilder.Id.")]
    public static partial void DuplicateId(ILogger logger, string commandId, string firstTitle, string secondTitle);

    [LoggerMessage(6, LogLevel.Warning, "The shortcut '{Shortcut}' is claimed by {Commands}; it runs '{Winner}', the one registered last.")]
    public static partial void ShortcutConflict(ILogger logger, string shortcut, string commands, string winner);

    [LoggerMessage(7, LogLevel.Warning, "The shortcut '{Shortcut}' of the command '{CommandId}' is also a toggle shortcut of the palette, which takes it: the command never runs from it.")]
    public static partial void ShortcutShadowedByToggle(ILogger logger, string shortcut, string commandId);

    [LoggerMessage(10, LogLevel.Warning, "The shortcut '{Shortcut}' of the command '{CommandId}' can never complete: '{Prefix}' is a shortcut of its own and fires first.")]
    public static partial void UnreachableSequence(ILogger logger, string shortcut, string commandId, string prefix);

    [LoggerMessage(12, LogLevel.Warning, "The shortcut '{Shortcut}' of the command '{CommandId}' is one browsers keep for themselves (closing or opening tabs and windows): a page never receives it.")]
    public static partial void BrowserReservedShortcut(ILogger logger, string shortcut, string commandId);

    [LoggerMessage(8, LogLevel.Warning, "Loading the command history failed; the palette starts without recents, pins and frecency.")]
    public static partial void HistoryLoadFailed(ILogger logger, Exception exception);

    [LoggerMessage(9, LogLevel.Warning, "Saving the command history failed; the change is kept for this session only.")]
    public static partial void HistorySaveFailed(ILogger logger, Exception exception);

    [LoggerMessage(13, LogLevel.Warning, "Bit.Balette's services are not registered, so the palette creates its own: it runs on the default BaletteOptions, and history goes to the browser's localStorage. Call builder.Services.AddBitBaletteServices() in Program.cs - in a Blazor Web App, in both the server and the client project. An app that lazy loads Bit.Balette on WebAssembly cannot, and can ignore this warning.")]
    public static partial void ServicesNotRegistered(ILogger logger);
}
