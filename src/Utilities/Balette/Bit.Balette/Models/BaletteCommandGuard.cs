namespace Bit.Balette;

/// <summary>What started a command: the palette's own UI, a global shortcut, or code calling <see cref="IBaletteService.ExecuteAsync"/>.</summary>
public enum BaletteExecutionTrigger
{
    /// <summary>A row chosen in the palette, with the keyboard or the pointer, or a prompt or multi-select page submitted.</summary>
    Palette,

    /// <summary>A command's global shortcut, pressed while the palette was closed (or open).</summary>
    Shortcut,

    /// <summary>The application, calling <see cref="IBaletteService.ExecuteAsync"/>.</summary>
    Programmatic,
}

/// <summary>
/// The answer of a guard (<see cref="IBaletteCommandGuard"/>, <see cref="BaletteCommand.Guard"/>): whether the command may
/// run, and - when it may not - what to tell the user. A <c>bool</c> converts to it, and <c>default</c> refuses, so a guard
/// that forgets to answer fails closed. Read <see cref="IsAllowed"/> rather than comparing results: two refusals with
/// different messages are different values, so <c>result == false</c> is false for <c>Deny("…")</c>.
/// </summary>
public readonly record struct BaletteGuardResult
{
    private BaletteGuardResult(bool isAllowed, string? message)
    {
        IsAllowed = isAllowed;
        Message = message;
    }

    /// <summary>Whether the command may run.</summary>
    public bool IsAllowed { get; }

    /// <summary>
    /// What the palette shows when the command was refused; null shows <see cref="BaletteStrings.CommandRefused"/>, and an
    /// empty string shows nothing.
    /// </summary>
    public string? Message { get; }

    /// <summary>The command may run.</summary>
    public static BaletteGuardResult Allow { get; } = new(true, null);

    /// <summary>The command may not run; <paramref name="message"/> is shown instead of <see cref="BaletteStrings.CommandRefused"/>.</summary>
    public static BaletteGuardResult Deny(string? message = null) => new(false, message);

    /// <summary><c>true</c> allows, <c>false</c> refuses with the default message.</summary>
    public static implicit operator BaletteGuardResult(bool allowed) => allowed ? Allow : Deny();
}

/// <summary>What a guard is asked about: the command about to run, what started it, and the scope's services.</summary>
public sealed class BaletteGuardContext
{
    internal BaletteGuardContext(BaletteCommand command, BaletteExecutionTrigger trigger, IServiceProvider services)
    {
        Command = command;
        Trigger = trigger;
        Services = services;
    }

    /// <summary>
    /// The command about to run - also when the guard is an ancestor's: a page's guard is asked about every command under
    /// it, so it sees which one.
    /// </summary>
    public BaletteCommand Command { get; }

    /// <summary>What started the command.</summary>
    public BaletteExecutionTrigger Trigger { get; }

    /// <summary>The scope's services - an authorization service, the signed-in user, a feature-flag client.</summary>
    public IServiceProvider Services { get; }
}

/// <summary>
/// Decides, every time a command is about to run, whether it may: the place for permission checks, feature flags and
/// tenancy rules that hold for many commands. Register one with <see cref="BitBalette.AddBitBaletteCommandGuard{TGuard}"/>;
/// it is resolved from the scope, so it can depend on scoped services such as an <c>AuthenticationStateProvider</c>.
/// </summary>
/// <remarks>
/// <para>
/// Guards run at execution time - after a row was chosen, a shortcut pressed or <see cref="IBaletteService.ExecuteAsync"/>
/// called, and again for the second <c>Enter</c> of a confirmation and when a prompt or multi-select page is submitted,
/// and before <see cref="IBaletteService.OpenAsync"/> opens a page by id - not while the list is built, which happens on
/// every keystroke. Hide what a user can never run with
/// <see cref="BaletteCommandBuilder.VisibleWhen"/>; enforce it with a guard.
/// </para>
/// <para>
/// A command's own guard and its ancestors' run first (outermost first), then every registered guard, and the first
/// refusal wins. A guard that throws refuses (it is logged). On WebAssembly a guard runs in the browser like the rest of
/// the app: it keeps the palette honest, and the server still has to check what a handler asks it to do.
/// </para>
/// </remarks>
public interface IBaletteCommandGuard
{
    /// <summary>Answers whether <see cref="BaletteGuardContext.Command"/> may run now.</summary>
    /// <param name="context">The command and what started it.</param>
    /// <param name="cancellationToken">Cancelled when the palette closes or is disposed while the guard is still deciding.</param>
    ValueTask<BaletteGuardResult> CheckAsync(BaletteGuardContext context, CancellationToken cancellationToken);
}

/// <summary>Describes a command a guard refused: <see cref="IBaletteService.CommandRefused"/>.</summary>
public sealed class BaletteCommandRefusedEventArgs(BaletteCommand command, BaletteExecutionTrigger trigger, string? message) : EventArgs
{
    /// <summary>The command that was not run.</summary>
    public BaletteCommand Command { get; } = command;

    /// <summary>What started it.</summary>
    public BaletteExecutionTrigger Trigger { get; } = trigger;

    /// <summary>The message the refusing guard gave, or null when it gave none.</summary>
    public string? Message { get; } = message;
}
