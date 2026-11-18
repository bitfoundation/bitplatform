namespace Bit.Balette;

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
