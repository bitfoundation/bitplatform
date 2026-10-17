using Microsoft.AspNetCore.Components;

namespace Bit.Balette;

/// <summary>Information passed to a command handler.</summary>
public sealed class BaletteCommandContext
{
    internal BaletteCommandContext(
        BaletteCommand command,
        string query,
        string? modePrefix,
        string? promptValue,
        IReadOnlyList<BaletteCommand> selected,
        IServiceProvider services,
        NavigationManager navigation,
        IBaletteService palette,
        CancellationToken cancellationToken)
    {
        Command = command;
        Query = query;
        ModePrefix = modePrefix;
        PromptValue = promptValue;
        SelectedCommands = selected;
        Services = services;
        Navigation = navigation;
        Palette = palette;
        CancellationToken = cancellationToken;
    }

    /// <summary>The command being executed.</summary>
    public BaletteCommand Command { get; }

    /// <summary>The search text at execution time (without a mode prefix).</summary>
    public string Query { get; }

    /// <summary>Active mode prefix (e.g. <c>&gt;</c>), or null for the default mode.</summary>
    public string? ModePrefix { get; }

    /// <summary>Text entered in a prompt step, when the command was reached via <see cref="BaletteCommandResult.Prompt"/>.</summary>
    public string? PromptValue { get; }

    /// <summary>In multi-select pages, the set of checked commands.</summary>
    public IReadOnlyList<BaletteCommand> SelectedCommands { get; }

    /// <summary>The scope's services.</summary>
    public IServiceProvider Services { get; }

    /// <summary>For handlers that navigate.</summary>
    public NavigationManager Navigation { get; }

    /// <summary>The palette service, for handlers that register, open or execute other commands.</summary>
    public IBaletteService Palette { get; }

    /// <summary>Cancelled when another command starts while this one is still running.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Resolves a service from the scope, throwing when it is not registered.</summary>
    public T GetRequiredService<T>() where T : notnull
        => (T)(Services.GetService(typeof(T)) ?? throw new InvalidOperationException($"Service {typeof(T)} is not registered."));
}
