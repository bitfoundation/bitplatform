namespace Bit.BevTools;

/// <summary>What a command can do to the BevTools UI.</summary>
public interface IBevToolsCommandContext
{
    /// <summary>Activates a panel by id ("components", "profiler", "timeline", "state", "forms", "routing", "network", "interop", "errors", "logs", "services", "diagnostics", "runtime", "browser", "about", or an extension panel id).</summary>
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
