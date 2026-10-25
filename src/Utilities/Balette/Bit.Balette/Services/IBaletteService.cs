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

    /// <summary>Adds a mode, replacing one with the same prefix. Dispose the returned handle to remove it.</summary>
    IDisposable RegisterMode(BaletteCommandMode mode);

    /// <summary>Adds an async source queried in the default mode. Dispose the returned handle to remove it.</summary>
    IDisposable RegisterSource(IBaletteSource source);

    /// <summary>
    /// Re-evaluates what depends on application state: every command's <see cref="BaletteCommand.IsVisible"/> predicate,
    /// which global shortcuts are claimed, and an open palette's list. Call it after state those predicates read changed -
    /// a selection, a signed-in user, the current route - so a command that just became available answers its shortcut
    /// and one that just went away leaves its keys to the page. Registering, updating or removing a command refreshes
    /// on its own.
    /// </summary>
    void Refresh()
    {
    }

    /// <summary>Opens the palette, optionally with an initial query (a mode prefix is honoured) or directly inside a command's page.</summary>
    Task OpenAsync(string? query = null, string? pageCommandId = null);

    /// <summary>Closes the palette, cancelling a command that is still running.</summary>
    Task CloseAsync();

    /// <summary>Opens the palette when it is closed and closes it when it is open.</summary>
    Task ToggleAsync();

    /// <summary>
    /// Executes a command programmatically as if selected - one from the registry, or one declared in the palette's
    /// markup or passed to its <c>Commands</c> parameter. A command that needs the palette (a page, a confirmation, a
    /// prompt) opens it. Throws <see cref="KeyNotFoundException"/> for an unknown id, and rethrows what the handler
    /// throws unless a <see cref="CommandFailed"/> subscriber set <see cref="BaletteCommandErrorEventArgs.Handled"/>.
    /// </summary>
    Task ExecuteAsync(string commandId);

    /// <summary>Fired whenever the registry changes.</summary>
    event Action? CommandsChanged;

    /// <summary>Fired after a palette opened.</summary>
    event Action? Opened;

    /// <summary>Fired after a palette closed.</summary>
    event Action? Closed;

    /// <summary>Fired after a command's handler returned, with its result and how long it took.</summary>
    event EventHandler<BaletteCommandExecutedEventArgs>? CommandExecuted;

    /// <summary>
    /// Fired when a command's handler, or the loader of a page's children, threw. The palette logs the exception and shows
    /// <see cref="BaletteCommandErrorEventArgs.Message"/>; a subscriber can change that message, or set
    /// <see cref="BaletteCommandErrorEventArgs.Handled"/> to say it reported the error itself.
    /// </summary>
    event EventHandler<BaletteCommandErrorEventArgs>? CommandFailed;

    /// <summary>
    /// Raised once the palette has detected the browser's platform, so UI that renders shortcut hints from
    /// <see cref="IsMac"/> can re-render with ⌘ instead of Ctrl.
    /// </summary>
    event Action? PlatformDetected;

    /// <summary>
    /// Raised when the first chords of a global shortcut sequence were pressed (<c>G</c> of <c>G S</c>), with them, and
    /// again with null when the sequence completed, broke off or timed out - for an app that shows the pending keys
    /// itself (see <see cref="BaletteOptions.ShowSequenceHint"/>).
    /// </summary>
    event Action<BaletteKeyboardShortcut?>? SequencePending
    {
        add { }
        remove { }
    }
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

    /// <summary>
    /// Set to true when the subscriber reported the error itself: the palette then does not log it, and
    /// <see cref="IBaletteService.ExecuteAsync"/> does not rethrow it. A failure in the palette's own UI - a row chosen
    /// with the keyboard or the mouse, a shortcut - is never rethrown, so it cannot end a Blazor Server circuit.
    /// </summary>
    public bool Handled { get; set; }

    /// <summary>
    /// What the palette shows the user: a localized, generic <see cref="BaletteStrings.CommandFailed"/> by default, so
    /// an exception's own text - which can name servers, tables or users - never reaches the screen by accident. Set it
    /// to a text of your own (for example <c>Exception.Message</c> for exceptions written for users), or to an empty
    /// string to show nothing.
    /// </summary>
    public string? Message { get; set; }
}
