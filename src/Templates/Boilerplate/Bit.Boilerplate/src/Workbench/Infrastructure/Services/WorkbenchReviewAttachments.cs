using System.Text.Json;
using System.Text.Json.Nodes;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>Keeps the page state saved with a comment or a flow screen, as a JSON file next to the review document.</summary>
public sealed partial class WorkbenchReviewAttachments
{
    private static string Folder => Path.Combine(WorkbenchFolders.Reviews, "States");

    /// <summary>Saves a state that is a JSON object within the snapshot size limit; returns whether it was saved.</summary>
    public async Task<bool> SaveState(string threadId, string? state)
    {
        if (string.IsNullOrEmpty(state) || state.Length > WorkbenchSnapshots.MaxLength || IsSafeId(threadId) is false)
            return false;

        try
        {
            if (JsonNode.Parse(state) is not JsonObject)
                return false;
        }
        catch (JsonException)
        {
            return false;
        }

        Directory.CreateDirectory(Folder);
        await File.WriteAllTextAsync(Path.Combine(Folder, $"{threadId}.json"), state);

        return true;
    }

    public async Task<string?> ReadState(string threadId)
    {
        var path = Path.Combine(Folder, $"{threadId}.json");

        return IsSafeId(threadId) && File.Exists(path) ? await File.ReadAllTextAsync(path) : null;
    }

    /// <summary>Deletes the state saved under an id, if there is one; a file still in use stays.</summary>
    public void DeleteState(string id)
    {
        var path = Path.Combine(Folder, $"{id}.json");

        if (IsSafeId(id) is false || File.Exists(path) is false)
            return;

        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
    }

    /// <summary>Ids are ASCII letters and digits only, so a file name made from one can't leave the folder.</summary>
    private static bool IsSafeId(string id) => id.Length is > 0 and <= 40 && id.All(char.IsAsciiLetterOrDigit);
}
