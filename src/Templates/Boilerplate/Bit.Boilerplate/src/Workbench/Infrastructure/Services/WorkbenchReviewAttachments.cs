using System.Text.Json;
using System.Text.Json.Nodes;

namespace Boilerplate.Workbench.Infrastructure.Services;

public sealed partial class WorkbenchReviewAttachments
{
    [AutoInject] private IHostEnvironment environment = default!;

    private string Folder => Path.Combine(environment.ContentRootPath, "App_Data", "Reviews", "States");

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

    private static bool IsSafeId(string id) => id.Length is > 0 and <= 40 && id.All(char.IsAsciiLetterOrDigit);
}
