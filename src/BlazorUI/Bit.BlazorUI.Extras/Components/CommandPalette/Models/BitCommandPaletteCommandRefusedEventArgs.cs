namespace Bit.BlazorUI;

/// <summary>Describes a command a guard refused: <see cref="IBitCommandPaletteService.CommandRefused"/>.</summary>
public sealed class BitCommandPaletteCommandRefusedEventArgs(BitCommandPaletteCommand command, BitCommandPaletteExecutionTrigger trigger, string? message) : EventArgs
{
    /// <summary>The command that was not run.</summary>
    public BitCommandPaletteCommand Command { get; } = command;

    /// <summary>What started it.</summary>
    public BitCommandPaletteExecutionTrigger Trigger { get; } = trigger;

    /// <summary>The message the refusing guard gave, or null when it gave none.</summary>
    public string? Message { get; } = message;
}
