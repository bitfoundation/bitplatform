namespace Bit.Balette;

/// <summary>
/// Scoped service that owns the command registry and the open/closed state of the palette.
/// Inject it anywhere to register commands, open the palette, or react to executions.
/// </summary>
public interface IBaletteService
{
    /// <summary>Whether a palette component is currently open.</summary>
    bool IsOpen { get; }

    /// <summary>True once the palette has detected a macOS / iOS browser (use it to render ⌘ instead of Ctrl).</summary>
    bool IsMac { get; }

    /// <summary>All registered commands (root and nested).</summary>
    IReadOnlyCollection<BaletteCommand> Commands { get; }

    /// <summary>Registered modes.</summary>
    IReadOnlyCollection<BaletteCommandMode> Modes { get; }

    /// <summary>Async sources used by the default mode.</summary>
    IReadOnlyCollection<IBaletteSource> Sources { get; }

    /// <summary>Registers commands. Dispose the returned handle to remove them (e.g. in a page's <c>Dispose</c>).</summary>
    IDisposable Register(params BaletteCommand[] commands);

    /// <summary>Registers commands. Dispose the returned handle to remove them.</summary>
    IDisposable Register(IEnumerable<BaletteCommand> commands);

    /// <summary>Replaces a command with the same <see cref="BaletteCommand.Id"/> (or adds it). Useful for toggles whose label changes.</summary>
    void Update(BaletteCommand command);

    /// <summary>Removes a command by id.</summary>
    bool Unregister(string commandId);

    /// <summary>Looks up a command by id across all registrations.</summary>
    BaletteCommand? Find(string commandId);

    IDisposable RegisterMode(BaletteCommandMode mode);

    IDisposable RegisterSource(IBaletteSource source);

    /// <summary>Opens the palette, optionally with an initial query (a mode prefix is honoured) or directly inside a command's page.</summary>
    Task OpenAsync(string? query = null, string? pageCommandId = null);

    Task CloseAsync();

    Task ToggleAsync();

    /// <summary>
    /// Executes a command programmatically as if selected - one from the registry, or one declared in the palette's
    /// markup or passed to its <c>Commands</c> parameter. A command that needs the palette (a page, a confirmation, a
    /// prompt) opens it. Throws <see cref="KeyNotFoundException"/> for an unknown id.
    /// </summary>
    Task ExecuteAsync(string commandId);

    /// <summary>Fired whenever the registry changes.</summary>
    event Action? CommandsChanged;

    event Action? Opened;

    event Action? Closed;

    event EventHandler<BaletteCommandExecutedEventArgs>? CommandExecuted;

    event EventHandler<BaletteCommandErrorEventArgs>? CommandFailed;

    /// <summary>
    /// Raised once the palette has detected the browser's platform, so UI that renders shortcut hints from
    /// <see cref="IsMac"/> can re-render with ⌘ instead of Ctrl.
    /// </summary>
    event Action? PlatformDetected;
}

/// <summary>Describes a command that ran: <see cref="IBaletteService.CommandExecuted"/> and <see cref="Balette.OnCommandExecuted"/>.</summary>
public sealed class BaletteCommandExecutedEventArgs(BaletteCommand command, BaletteCommandResult result, TimeSpan duration) : EventArgs
{
    /// <summary>The command.</summary>
    public BaletteCommand Command { get; } = command;

    /// <summary>What the handler answered.</summary>
    public BaletteCommandResult Result { get; } = result;

    /// <summary>How long the handler took.</summary>
    public TimeSpan Duration { get; } = duration;
}

/// <summary>Describes a handler that threw: <see cref="IBaletteService.CommandFailed"/>.</summary>
public sealed class BaletteCommandErrorEventArgs(BaletteCommand command, Exception exception) : EventArgs
{
    /// <summary>The command.</summary>
    public BaletteCommand Command { get; } = command;

    /// <summary>What the handler threw.</summary>
    public Exception Exception { get; } = exception;

    /// <summary>Set to true to prevent the exception from being rethrown.</summary>
    public bool Handled { get; set; }
}
