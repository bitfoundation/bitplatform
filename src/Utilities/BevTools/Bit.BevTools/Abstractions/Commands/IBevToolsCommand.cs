namespace Bit.BevTools;

/// <summary>A command shown in the BevTools command palette.</summary>
public interface IBevToolsCommand
{
    string Id { get; }

    string Title { get; }

    /// <summary>Grouping label, e.g. "Components", "Network".</summary>
    string Category { get; }

    /// <summary>Extra words that make the command findable.</summary>
    IReadOnlyList<string> Keywords => Array.Empty<string>();

    /// <summary>True when the command accepts free text after its title, e.g. "Find component ProductList".</summary>
    bool AcceptsArgument => false;

    Task ExecuteAsync(IBevToolsCommandContext context, string? argument);
}
