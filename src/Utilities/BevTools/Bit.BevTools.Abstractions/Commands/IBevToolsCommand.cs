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

/// <summary>What a command can do to the BevTools UI.</summary>
public interface IBevToolsCommandContext
{
    /// <summary>Activates a panel by id ("components", "profiler", "timeline", "state", "network", "interop", "errors", "services", "diagnostics", "browser", or an extension panel id).</summary>
    void ShowPanel(string panelId, string? query = null);

    void SelectComponent(long instanceId);

    void SelectEvent(long eventId);

    void Notify(string message);

    /// <summary>Persists the current panel preferences when the command changes UI state directly.</summary>
    void PersistPreferences()
    {
    }

    /// <summary>Re-reads captured data and re-renders the open tab.</summary>
    void RefreshView()
    {
    }

    /// <summary>Copies text to the clipboard through the browser bridge. Returns false when the bridge is unavailable.</summary>
    Task<bool> CopyTextAsync(string text) => Task.FromResult(false);

    /// <summary>Offers a text file for download through the browser bridge. Returns false when the bridge is unavailable.</summary>
    Task<bool> DownloadTextAsync(string fileName, string content, string contentType = "text/plain") => Task.FromResult(false);

    IServiceProvider Services { get; }
}
