using System.Text.Json;
using System.Text.Json.Nodes;

namespace Bit.Cli.Infrastructure;

public sealed class CliSettings
{
    private readonly string path;
    private readonly JsonObject root;

    private CliSettings(string path, JsonObject root)
    {
        this.path = path;
        this.root = root;
    }

    public static CliSettings Load(CliEnvironment environment)
    {
        var path = Path.Combine(environment.BitDirectory, "settings.json");

        try
        {
            if (File.Exists(path) && JsonNode.Parse(File.ReadAllText(path)) is JsonObject existing)
                return new CliSettings(path, existing);
        }
        catch (Exception exp) when (exp is JsonException or IOException or UnauthorizedAccessException)
        {
        }

        return new CliSettings(path, []);
    }

    public string? Telemetry
    {
        get => GetString("telemetry");
        set => root["telemetry"] = value;
    }

    public int TelemetryNoticeVersion
    {
        get => root["telemetryNoticeVersion"]?.GetValueKind() is JsonValueKind.Number ? root["telemetryNoticeVersion"]!.GetValue<int>() : 0;
        set => root["telemetryNoticeVersion"] = value;
    }

    public bool UsageQuestionAsked
    {
        get => root["usageQuestionAsked"]?.GetValueKind() is JsonValueKind.True;
        set => root["usageQuestionAsked"] = value;
    }

    public bool IsFirstRun { get; private set; }

    public string InstallId
    {
        get
        {
            if (GetString("installId") is { Length: > 0 } existing)
                return existing;

            IsFirstRun = true;
            var created = Guid.NewGuid().ToString("N");
            root["installId"] = created;
            return created;
        }
    }

    public bool Save()
    {
        try
        {
            AtomicFile.WriteAllText(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            return true;
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private string? GetString(string key)
    {
        return root[key]?.GetValueKind() is JsonValueKind.String ? root[key]!.GetValue<string>() : null;
    }
}
