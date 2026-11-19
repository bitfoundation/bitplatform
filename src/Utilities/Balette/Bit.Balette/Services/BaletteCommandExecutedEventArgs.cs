namespace Bit.Balette;

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
