using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tools;
using Microsoft.Data.Sqlite;
using Tomlyn;
using Tomlyn.Model;

namespace Bit.Cli.Trust;

public enum TrustResultKind
{
    Trusted,
    AlreadyTrusted,
    NotInstalled,
    Skipped,
    Failed
}

public sealed record TrustOutcome(string Tool, TrustResultKind Kind, string? Detail = null);

public sealed class TrustContext(CliEnvironment environment, IProcessRunner runner)
{
    public CliEnvironment Environment { get; } = environment;

    public IProcessRunner Runner { get; } = runner;

    public Func<bool> IsVsCodeRunning { get; init; } = () => VsCodeTrust.IsAnyVsCodeRunning();

    public string Home(string? overrideVariable, string folderName)
    {
        return overrideVariable is not null && Environment.GetVariable(overrideVariable) is { } overridden
            ? overridden
            : Path.Combine(Environment.HomeDirectory, folderName);
    }
}

public abstract class TrustWriter
{
    private static readonly JsonSerializerOptions indented = new() { WriteIndented = true };

    public abstract string Name { get; }

    public abstract bool IsInstalled(TrustContext context);

    public TrustOutcome Trust(TrustContext context, string folder)
    {
        if (IsInstalled(context) is false)
            return new TrustOutcome(Name, TrustResultKind.NotInstalled);

        try
        {
            return TrustInstalled(context, NormalizeFolder(folder, context.Environment));
        }
        catch (Exception exp) when (exp is IOException or UnauthorizedAccessException or JsonException or InvalidOperationException or SqliteException or TomlException)
        {
            return new TrustOutcome(Name, TrustResultKind.Failed, exp.Message);
        }
    }

    protected abstract TrustOutcome TrustInstalled(TrustContext context, string folder);

    public static string NormalizeFolder(string folder, CliEnvironment environment)
    {
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        return environment.IsWindows && full.Length >= 2 && full[1] == ':' ? char.ToUpperInvariant(full[0]) + full[1..] : full;
    }

    protected static StringComparison PathComparison(TrustContext context) => context.Environment.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    protected static JsonObject ReadJsonObject(string path)
    {
        if (File.Exists(path) is false)
            return [];

        var text = File.ReadAllText(path);

        if (string.IsNullOrWhiteSpace(text))
            return [];

        return JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true }) as JsonObject
            ?? throw new InvalidOperationException($"{path} isn't a JSON object, so it was left alone.");
    }

    protected static void WriteJson(string path, JsonNode node)
    {
        AtomicFile.WriteAllText(path, node.ToJsonString(indented) + System.Environment.NewLine);
    }
}

public sealed class ClaudeCodeTrust : TrustWriter
{
    public override string Name => "Claude Code";

    private static string ConfigPath(TrustContext context)
    {
        return context.Environment.GetVariable("CLAUDE_CONFIG_DIR") is { } directory
            ? Path.Combine(directory, ".claude.json")
            : Path.Combine(context.Environment.HomeDirectory, ".claude.json");
    }

    public override bool IsInstalled(TrustContext context) => File.Exists(ConfigPath(context)) || context.Runner.FindExecutable("claude") is not null;

    protected override TrustOutcome TrustInstalled(TrustContext context, string folder)
    {
        var path = ConfigPath(context);
        var root = ReadJsonObject(path);

        if (root["projects"] is not JsonObject projects)
        {
            root["projects"] = projects = [];
        }

        var keys = ProjectKeys(folder, context.Environment);

        if (keys.All(k => projects[k] is JsonObject existing && existing["hasTrustDialogAccepted"]?.GetValueKind() is JsonValueKind.True))
            return new TrustOutcome(Name, TrustResultKind.AlreadyTrusted);

        foreach (var key in keys)
        {
            if (projects[key] is not JsonObject project)
            {
                projects[key] = project = new JsonObject
                {
                    ["allowedTools"] = new JsonArray(),
                    ["mcpContextUris"] = new JsonArray(),
                    ["enabledMcpjsonServers"] = new JsonArray(),
                    ["disabledMcpjsonServers"] = new JsonArray(),
                    ["hasClaudeMdExternalIncludesApproved"] = false,
                    ["hasClaudeMdExternalIncludesWarningShown"] = false
                };
            }

            project["hasTrustDialogAccepted"] = true;
        }

        WriteJson(path, root);
        return new TrustOutcome(Name, TrustResultKind.Trusted, path);
    }

    public static IReadOnlyList<string> ProjectKeys(string folder, CliEnvironment environment)
    {
        if (environment.IsWindows is false || folder.Length < 2 || folder[1] != ':')
            return [folder];

        var forward = folder.Replace('\\', '/');

        return [.. new[] { forward, folder }.SelectMany(k => new[] { char.ToUpperInvariant(k[0]) + k[1..], char.ToLowerInvariant(k[0]) + k[1..] })];
    }
}

public sealed class CopilotCliTrust : TrustWriter
{
    public override string Name => "Copilot CLI";

    private static string ConfigPath(TrustContext context) => Path.Combine(context.Home("COPILOT_HOME", ".copilot"), "config.json");

    public override bool IsInstalled(TrustContext context) => File.Exists(ConfigPath(context)) || context.Runner.FindExecutable("copilot") is not null;

    protected override TrustOutcome TrustInstalled(TrustContext context, string folder)
    {
        var path = ConfigPath(context);
        var root = ReadJsonObject(path);

        if (root["trustedFolders"] is not JsonArray folders)
        {
            root["trustedFolders"] = folders = [];
        }

        var comparison = PathComparison(context);
        if (folders.Any(f => f?.GetValueKind() is JsonValueKind.String && (string.Equals(f.GetValue<string>(), folder, comparison) || folder.StartsWith(Path.TrimEndingDirectorySeparator(f.GetValue<string>()) + Path.DirectorySeparatorChar, comparison))))
            return new TrustOutcome(Name, TrustResultKind.AlreadyTrusted);

        folders.Add(folder);
        WriteJson(path, root);
        return new TrustOutcome(Name, TrustResultKind.Trusted, path);
    }
}

public sealed class CodexTrust : TrustWriter
{
    public override string Name => "Codex";

    private static string ConfigPath(TrustContext context) => Path.Combine(context.Home("CODEX_HOME", ".codex"), "config.toml");

    public override bool IsInstalled(TrustContext context) => File.Exists(ConfigPath(context)) || context.Runner.FindExecutable("codex") is not null;

    protected override TrustOutcome TrustInstalled(TrustContext context, string folder)
    {
        var path = ConfigPath(context);
        var text = File.Exists(path) ? File.ReadAllText(path) : "";

        if (text.Length > 0)
        {
            var model = TomlSerializer.Deserialize<TomlTable>(text);

            if (model is not null && model.TryGetValue("projects", out var projectsValue) && projectsValue is TomlTable projects)
            {
                foreach (var (key, value) in projects)
                {
                    if (string.Equals(Path.TrimEndingDirectorySeparator(key), folder, PathComparison(context)) is false)
                        continue;

                    return value is TomlTable project && project.TryGetValue("trust_level", out var level) && level is "trusted"
                        ? new TrustOutcome(Name, TrustResultKind.AlreadyTrusted)
                        : new TrustOutcome(Name, TrustResultKind.Skipped, "Codex already has a trust setting of yours for this folder");
                }
            }
        }

        var tableKey = folder.Contains('\'') ? $"\"{folder.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"" : $"'{folder}'";
        var separator = text.Length == 0 || text.EndsWith('\n') ? "" : System.Environment.NewLine;
        var updated = $"{text}{separator}{(text.Length == 0 ? "" : System.Environment.NewLine)}[projects.{tableKey}]{System.Environment.NewLine}trust_level = \"trusted\"{System.Environment.NewLine}";

        TomlSerializer.Deserialize<TomlTable>(updated);
        AtomicFile.WriteAllText(path, updated);
        return new TrustOutcome(Name, TrustResultKind.Trusted, path);
    }
}

public sealed class GeminiTrust : TrustWriter
{
    public override string Name => "Gemini CLI";

    private static string ConfigPath(TrustContext context) => Path.Combine(context.Environment.HomeDirectory, ".gemini", "trustedFolders.json");

    public override bool IsInstalled(TrustContext context) => Directory.Exists(Path.GetDirectoryName(ConfigPath(context))) || context.Runner.FindExecutable("gemini") is not null;

    protected override TrustOutcome TrustInstalled(TrustContext context, string folder)
    {
        var path = ConfigPath(context);
        var root = ReadJsonObject(path);

        if (root.Any(p => string.Equals(p.Key, folder, PathComparison(context)) && p.Value?.GetValueKind() is JsonValueKind.String && p.Value.GetValue<string>() is "TRUST_FOLDER"))
            return new TrustOutcome(Name, TrustResultKind.AlreadyTrusted);

        root[folder] = "TRUST_FOLDER";
        WriteJson(path, root);
        return new TrustOutcome(Name, TrustResultKind.Trusted, path);
    }
}

public sealed class VsCodeTrust : TrustWriter
{
    public const string TrustKey = "content.trust.model.key";

    public override string Name => "VS Code";

    public override bool IsInstalled(TrustContext context) => IdeLocator.FindVsCode(context.Environment, context.Runner) is not null;

    public static IReadOnlyList<string> LegacyStores(CliEnvironment environment)
    {
        var flavors = new[] { "Code", "Code - Insiders" };

        return environment.Os switch
        {
            HostOs.Windows => [.. flavors.Select(f => Path.Combine(environment.GetVariable("APPDATA") ?? Path.Combine(environment.HomeDirectory, "AppData", "Roaming"), f, "User", "globalStorage", "state.vscdb"))],
            HostOs.MacOS => [.. flavors.Select(f => Path.Combine(environment.HomeDirectory, "Library", "Application Support", f, "User", "globalStorage", "state.vscdb"))],
            _ => [.. flavors.Select(f => Path.Combine(environment.GetVariable("XDG_CONFIG_HOME") ?? Path.Combine(environment.HomeDirectory, ".config"), f, "User", "globalStorage", "state.vscdb"))]
        };
    }

    public static string SharedStore(CliEnvironment environment) => Path.Combine(environment.HomeDirectory, ".vscode-shared", "sharedStorage", "state.vscdb");

    protected override TrustOutcome TrustInstalled(TrustContext context, string folder)
    {
        if (context.IsVsCodeRunning())
            return new TrustOutcome(Name, TrustResultKind.Skipped, "VS Code is running, so its trust store was left alone");

        var shared = SharedStore(context.Environment);
        var stores = File.Exists(shared)
            ? [(shared, true)]
            : LegacyStores(context.Environment).Where(File.Exists).Select(p => (p, false)).ToList();

        if (stores.Count == 0)
        {
            CreateStore(shared);
            stores = [(shared, true)];
        }

        var outcome = new TrustOutcome(Name, TrustResultKind.Skipped, "the trust store's format isn't one bit knows");

        foreach (var (store, createKey) in stores)
        {
            var result = TrustInStore(store, folder, context.Environment, createKey);

            if (result.Kind is TrustResultKind.Trusted or TrustResultKind.AlreadyTrusted)
            {
                outcome = result;
            }
        }

        return outcome;
    }

    private static void CreateStore(string store)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(store)!);
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = store, Mode = SqliteOpenMode.ReadWriteCreate, Pooling = false }.ToString());
        connection.Open();
        using var create = connection.CreateCommand();
        create.CommandText = "CREATE TABLE IF NOT EXISTS ItemTable (key TEXT UNIQUE ON CONFLICT REPLACE, value BLOB)";
        create.ExecuteNonQuery();
    }

    public TrustOutcome TrustInStore(string store, string folder, CliEnvironment environment, bool createKey)
    {
        using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = store, Mode = SqliteOpenMode.ReadWrite, Pooling = false }.ToString());
        connection.Open();

        using (var schema = connection.CreateCommand())
        {
            schema.CommandText = "SELECT COUNT(*) FROM pragma_table_info('ItemTable') WHERE name IN ('key', 'value')";

            if (Convert.ToInt32(schema.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) != 2)
                return new TrustOutcome(Name, TrustResultKind.Skipped, "the trust store's format isn't one bit knows");
        }

        string? existing;
        using (var read = connection.CreateCommand())
        {
            read.CommandText = "SELECT value FROM ItemTable WHERE key = $key";
            read.Parameters.AddWithValue("$key", TrustKey);
            existing = read.ExecuteScalar() switch
            {
                string s => s,
                byte[] bytes => System.Text.Encoding.UTF8.GetString(bytes),
                _ => null
            };
        }

        if (existing is null && createKey is false)
            return new TrustOutcome(Name, TrustResultKind.Skipped, "the trust store's format isn't one bit knows");

        var model = existing is null ? new JsonObject { ["uriTrustInfo"] = new JsonArray() } : JsonNode.Parse(existing) as JsonObject;

        if (model is null || model.Count != 1 || model["uriTrustInfo"] is not JsonArray entries || entries.Any(e => IsFamiliarEntry(e) is false))
            return new TrustOutcome(Name, TrustResultKind.Skipped, "the trust store's format isn't one bit knows");

        var uri = CreateUri(folder, environment);
        var path = uri["path"]!.GetValue<string>();
        var comparison = environment.IsWindows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        if (entries.Any(e => e!["trusted"]!.GetValue<bool>() && e["uri"]!["path"]!.GetValue<string>() is { } trustedPath && (string.Equals(trustedPath, path, comparison) || path.StartsWith(trustedPath.TrimEnd('/') + "/", comparison))))
            return new TrustOutcome(Name, TrustResultKind.AlreadyTrusted);

        entries.Add(new JsonObject { ["uri"] = uri, ["trusted"] = true });

        using (var write = connection.CreateCommand())
        {
            write.CommandText = "INSERT OR REPLACE INTO ItemTable (key, value) VALUES ($key, $value)";
            write.Parameters.AddWithValue("$key", TrustKey);
            write.Parameters.AddWithValue("$value", model.ToJsonString());
            write.ExecuteNonQuery();
        }

        return new TrustOutcome(Name, TrustResultKind.Trusted, store);
    }

    public static JsonObject CreateUri(string folder, CliEnvironment environment)
    {
        if (environment.IsWindows && folder.Length >= 2 && folder[1] == ':')
        {
            var drive = char.ToLowerInvariant(folder[0]);
            var rest = folder[2..].Replace('\\', '/');
            var segments = rest.Split('/', StringSplitOptions.RemoveEmptyEntries);

            return new JsonObject
            {
                ["$mid"] = 1,
                ["fsPath"] = $"{drive}:{folder[2..]}",
                ["_sep"] = 1,
                ["external"] = $"file:///{drive}%3A/{string.Join('/', segments.Select(Uri.EscapeDataString))}",
                ["path"] = $"/{drive}:/{string.Join('/', segments)}",
                ["scheme"] = "file"
            };
        }

        var unixSegments = folder.Split('/', StringSplitOptions.RemoveEmptyEntries);

        return new JsonObject
        {
            ["$mid"] = 1,
            ["fsPath"] = folder,
            ["external"] = $"file:///{string.Join('/', unixSegments.Select(Uri.EscapeDataString))}",
            ["path"] = "/" + string.Join('/', unixSegments),
            ["scheme"] = "file"
        };
    }

    public static bool IsAnyVsCodeRunning()
    {
        try
        {
            foreach (var process in Process.GetProcesses())
            {
                using (process)
                {
                    var name = process.ProcessName;

                    if (name.Equals("Code", StringComparison.OrdinalIgnoreCase)
                        || name.Equals("Code - Insiders", StringComparison.OrdinalIgnoreCase)
                        || name.Equals("code-insiders", StringComparison.OrdinalIgnoreCase)
                        || name.StartsWith("Code Helper", StringComparison.OrdinalIgnoreCase))
                        return true;

                    if (OperatingSystem.IsMacOS() && name is "Electron")
                    {
                        try
                        {
                            if (process.MainModule?.FileName.Contains("Visual Studio Code", StringComparison.OrdinalIgnoreCase) is true)
                                return true;
                        }
                        catch (Exception)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static bool IsFamiliarEntry(JsonNode? entry)
    {
        return entry is JsonObject item
            && item["trusted"]?.GetValueKind() is JsonValueKind.True or JsonValueKind.False
            && item["uri"] is JsonObject uri
            && uri["scheme"]?.GetValueKind() is JsonValueKind.String
            && uri["path"]?.GetValueKind() is JsonValueKind.String;
    }
}

public static class TrustWriters
{
    public static IReadOnlyList<TrustWriter> All { get; } = [new VsCodeTrust(), new ClaudeCodeTrust(), new CopilotCliTrust(), new CodexTrust(), new GeminiTrust()];
}
