namespace Bit.BlazorUI;

/// <summary>Describes a command that ran: <see cref="IBitCommandPaletteService.CommandExecuted"/> and <c>BitCommandPalette.OnCommandExecuted</c>.</summary>
public sealed class BitCommandPaletteCommandExecutedEventArgs(BitCommandPaletteCommand command, BitCommandPaletteCommandResult result, TimeSpan duration) : EventArgs
{
    /// <summary>The command.</summary>
    public BitCommandPaletteCommand Command { get; } = command;

    /// <summary>What the handler answered.</summary>
    public BitCommandPaletteCommandResult Result { get; } = result;

    /// <summary>How long the handler took.</summary>
    public TimeSpan Duration { get; } = duration;
}
