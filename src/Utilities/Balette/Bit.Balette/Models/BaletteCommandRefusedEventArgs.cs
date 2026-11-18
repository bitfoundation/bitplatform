namespace Bit.Balette;

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
