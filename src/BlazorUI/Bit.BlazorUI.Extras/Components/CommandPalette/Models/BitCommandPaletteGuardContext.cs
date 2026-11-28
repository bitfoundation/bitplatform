namespace Bit.BlazorUI;

/// <summary>What a guard is asked about: the command about to run, what started it, and the scope's services.</summary>
public sealed class BitCommandPaletteGuardContext
{
    internal BitCommandPaletteGuardContext(BitCommandPaletteCommand command, BitCommandPaletteExecutionTrigger trigger, IServiceProvider services)
    {
        Command = command;
        Trigger = trigger;
        Services = services;
    }

    /// <summary>
    /// The command about to run - also when the guard is an ancestor's: a page's guard is asked about every command under
    /// it, so it sees which one.
    /// </summary>
    public BitCommandPaletteCommand Command { get; }

    /// <summary>What started the command.</summary>
    public BitCommandPaletteExecutionTrigger Trigger { get; }

    /// <summary>The scope's services - an authorization service, the signed-in user, a feature-flag client.</summary>
    public IServiceProvider Services { get; }
}
