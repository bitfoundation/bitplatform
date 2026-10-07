using System.Text.Json.Nodes;
using Bit.Cli.Infrastructure;
using Bit.Cli.Tests.Infrastructure;
using Bit.Cli.Trust;
using Microsoft.Data.Sqlite;
using Tomlyn;
using Tomlyn.Model;

namespace Bit.Cli.Tests;

[TestClass]
public class TrustTests
{
    [TestMethod]
    public void ToolsThatArentInstalled_Should_BeLeftAlone()
    {
        using var host = new TestHost();
        var context = new TrustContext(host.Environment, host.Runner) { IsVsCodeRunning = () => false };

        foreach (var writer in TrustWriters.All.Where(w => w is not VsCodeTrust || Bit.Cli.Tools.IdeLocator.FindVsCode(host.Environment, host.Runner) is null))
        {
            Assert.AreEqual(TrustResultKind.NotInstalled, writer.Trust(context, ProjectFolder(host)).Kind, writer.Name);
        }

        Assert.IsFalse(File.Exists(Path.Combine(host.Home, ".claude.json")));
    }

    [TestMethod]
    public void ClaudeCode_Should_TrustEveryKeyItLooksUpAndKeepEverythingElse()
    {
        using var host = new TestHost();
        var config = Path.Combine(host.Home, ".claude.json");
        File.WriteAllText(config, """{ "userID": "abc", "projects": { "C:\\other": { "hasTrustDialogAccepted": false, "allowedTools": ["x"] } } }""");
        var folder = ProjectFolder(host);
        var context = new TrustContext(host.Environment, host.Runner);

        var outcome = new ClaudeCodeTrust().Trust(context, folder);

        Assert.AreEqual(TrustResultKind.Trusted, outcome.Kind);

        var root = JsonNode.Parse(File.ReadAllText(config))!;

        Assert.AreEqual("abc", root["userID"]!.GetValue<string>());
        Assert.AreEqual("x", root["projects"]!["C:\\other"]!["allowedTools"]![0]!.GetValue<string>());

        foreach (var key in ClaudeCodeTrust.ProjectKeys(TrustWriter.NormalizeFolder(folder, host.Environment), host.Environment))
        {
            Assert.IsTrue(root["projects"]![key]!["hasTrustDialogAccepted"]!.GetValue<bool>(), key);
        }

        Assert.AreEqual(TrustResultKind.AlreadyTrusted, new ClaudeCodeTrust().Trust(context, folder).Kind);
    }

    [TestMethod]
    public void ClaudeCode_Should_KeyAWindowsFolderTheWayClaudeCodeLooksItUp()
    {
        using var windows = new TestHost(HostOs.Windows);
        using var linux = new TestHost(HostOs.Linux);

        CollectionAssert.AreEqual(new[] { "D:/Sources/Contoso", "d:/Sources/Contoso", "D:\\Sources\\Contoso", "d:\\Sources\\Contoso" }, ClaudeCodeTrust.ProjectKeys("D:\\Sources\\Contoso", windows.Environment).ToArray());
        CollectionAssert.AreEqual(new[] { "/home/me/Contoso" }, ClaudeCodeTrust.ProjectKeys("/home/me/Contoso", linux.Environment).ToArray());
    }

    [TestMethod]
    public void ClaudeCode_Should_HonorClaudeConfigDir()
    {
        var customDirectory = Directory.CreateTempSubdirectory("bit-cli-claude-").FullName;
        using var custom = new TestHost(variables: new Dictionary<string, string> { ["CLAUDE_CONFIG_DIR"] = customDirectory });
        custom.Runner.Executables["claude"] = typeof(TrustTests).Assembly.Location;

        var outcome = new ClaudeCodeTrust().Trust(new TrustContext(custom.Environment, custom.Runner), ProjectFolder(custom));

        Assert.AreEqual(TrustResultKind.Trusted, outcome.Kind);
        Assert.IsTrue(File.Exists(Path.Combine(customDirectory, ".claude.json")));
        Assert.IsFalse(File.Exists(Path.Combine(custom.Home, ".claude.json")));
        Directory.Delete(customDirectory, recursive: true);
    }

    [TestMethod]
    public void CopilotCli_Should_AddTheFolderOnceAndRespectTrustedParents()
    {
        using var host = new TestHost();
        var config = Path.Combine(Directory.CreateDirectory(Path.Combine(host.Home, ".copilot")).FullName, "config.json");
        File.WriteAllText(config, """{ "theme": "dark", "trustedFolders": [] }""");
        var context = new TrustContext(host.Environment, host.Runner);
        var folder = ProjectFolder(host);

        Assert.AreEqual(TrustResultKind.Trusted, new CopilotCliTrust().Trust(context, folder).Kind);
        Assert.AreEqual(TrustResultKind.AlreadyTrusted, new CopilotCliTrust().Trust(context, folder).Kind);

        var root = JsonNode.Parse(File.ReadAllText(config))!;
        Assert.AreEqual("dark", root["theme"]!.GetValue<string>());
        Assert.HasCount(1, root["trustedFolders"]!.AsArray());

        var child = Directory.CreateDirectory(Path.Combine(folder, "src")).FullName;
        Assert.AreEqual(TrustResultKind.AlreadyTrusted, new CopilotCliTrust().Trust(context, child).Kind);
    }

    [TestMethod]
    public void Codex_Should_AppendAProjectTableThatStillParses()
    {
        using var host = new TestHost();
        var config = Path.Combine(Directory.CreateDirectory(Path.Combine(host.Home, ".codex")).FullName, "config.toml");
        File.WriteAllText(config, "model = \"gpt-5.2-codex\"\n\n[mcp_servers.docs]\ncommand = \"npx\"\n");
        var context = new TrustContext(host.Environment, host.Runner);
        var folder = ProjectFolder(host);

        Assert.AreEqual(TrustResultKind.Trusted, new CodexTrust().Trust(context, folder).Kind);
        Assert.AreEqual(TrustResultKind.AlreadyTrusted, new CodexTrust().Trust(context, folder).Kind);

        var text = File.ReadAllText(config);
        StringAssert.StartsWith(text, "model = \"gpt-5.2-codex\"\n\n[mcp_servers.docs]\ncommand = \"npx\"\n");

        var model = TomlSerializer.Deserialize<TomlTable>(text)!;
        var projects = (TomlTable)model["projects"];
        var project = (TomlTable)projects[TrustWriter.NormalizeFolder(folder, host.Environment)];

        Assert.AreEqual("trusted", project["trust_level"]);
        Assert.AreEqual("npx", ((TomlTable)((TomlTable)model["mcp_servers"])["docs"])["command"]);
    }

    [TestMethod]
    public void Codex_Should_KeepTheUsersOwnDecision()
    {
        using var host = new TestHost();
        var folder = ProjectFolder(host);
        var key = TrustWriter.NormalizeFolder(folder, host.Environment).Replace("\\", "\\\\", StringComparison.Ordinal);
        var config = Path.Combine(Directory.CreateDirectory(Path.Combine(host.Home, ".codex")).FullName, "config.toml");
        File.WriteAllText(config, $"[projects.\"{key}\"]\ntrust_level = \"untrusted\"\n");

        var outcome = new CodexTrust().Trust(new TrustContext(host.Environment, host.Runner), folder);

        Assert.AreEqual(TrustResultKind.Skipped, outcome.Kind);
        StringAssert.Contains(File.ReadAllText(config), "untrusted");
    }

    [TestMethod]
    public void Gemini_Should_MarkTheFolder()
    {
        using var host = new TestHost();
        Directory.CreateDirectory(Path.Combine(host.Home, ".gemini"));
        var folder = ProjectFolder(host);

        Assert.AreEqual(TrustResultKind.Trusted, new GeminiTrust().Trust(new TrustContext(host.Environment, host.Runner), folder).Kind);

        var root = JsonNode.Parse(File.ReadAllText(Path.Combine(host.Home, ".gemini", "trustedFolders.json")))!;
        Assert.AreEqual("TRUST_FOLDER", root[TrustWriter.NormalizeFolder(folder, host.Environment)]!.GetValue<string>());
    }

    [TestMethod]
    public void VsCode_Should_AddAnEntryToAStoreItKnows()
    {
        using var host = new TestHost();
        var store = CreateStore(host, """{"uriTrustInfo":[{"uri":{"$mid":1,"fsPath":"/tmp/other","external":"file:///tmp/other","path":"/tmp/other","scheme":"file"},"trusted":true}]}""");
        var folder = ProjectFolder(host);

        var outcome = new VsCodeTrust().TrustInStore(store, folder, host.Environment, createKey: true);

        Assert.AreEqual(TrustResultKind.Trusted, outcome.Kind);

        var entries = JsonNode.Parse(ReadStore(store)!)!["uriTrustInfo"]!.AsArray();
        Assert.HasCount(2, entries);
        Assert.AreEqual(VsCodeTrust.CreateUri(TrustWriter.NormalizeFolder(folder, host.Environment), host.Environment)["path"]!.GetValue<string>(), entries[1]!["uri"]!["path"]!.GetValue<string>());
        Assert.AreEqual(TrustResultKind.AlreadyTrusted, new VsCodeTrust().TrustInStore(store, folder, host.Environment, createKey: true).Kind);
    }

    [TestMethod]
    public void VsCode_Should_LeaveAnUnfamiliarStoreAlone()
    {
        using var host = new TestHost();
        var value = """{"uriTrustInfo":[{"location":"/x","trusted":true}],"version":2}""";
        var store = CreateStore(host, value);

        var outcome = new VsCodeTrust().TrustInStore(store, ProjectFolder(host), host.Environment, createKey: true);

        Assert.AreEqual(TrustResultKind.Skipped, outcome.Kind);
        Assert.AreEqual(value, ReadStore(store));
    }

    [TestMethod]
    public void VsCode_Should_NotTouchTheStoreWhileItRuns()
    {
        using var host = new TestHost();
        host.Runner.Executables["code"] = typeof(TrustTests).Assembly.Location;
        var store = CreateStore(host, null);
        var context = new TrustContext(host.Environment, host.Runner) { IsVsCodeRunning = () => true };

        var outcome = new VsCodeTrust().Trust(context, ProjectFolder(host));

        Assert.AreEqual(TrustResultKind.Skipped, outcome.Kind);
        Assert.IsNull(ReadStore(store));
    }

    [TestMethod]
    public void VsCode_Should_TrustAFolderUnderATrustedParent()
    {
        using var host = new TestHost();
        var parent = TrustWriter.NormalizeFolder(host.WorkingDirectory, host.Environment);
        var parentUri = VsCodeTrust.CreateUri(parent, host.Environment);
        var store = CreateStore(host, new JsonObject { ["uriTrustInfo"] = new JsonArray(new JsonObject { ["uri"] = parentUri, ["trusted"] = true }) }.ToJsonString());

        Assert.AreEqual(TrustResultKind.AlreadyTrusted, new VsCodeTrust().TrustInStore(store, ProjectFolder(host), host.Environment, createKey: true).Kind);
    }

    [TestMethod]
    public void VsCodeUris_Should_LookLikeTheOnesVsCodeWrites()
    {
        using var windows = new TestHost(HostOs.Windows);
        var uri = VsCodeTrust.CreateUri(@"D:\Projects\My App", windows.Environment);

        Assert.AreEqual(@"d:\Projects\My App", uri["fsPath"]!.GetValue<string>());
        Assert.AreEqual("/d:/Projects/My App", uri["path"]!.GetValue<string>());
        Assert.AreEqual("file:///d%3A/Projects/My%20App", uri["external"]!.GetValue<string>());
        Assert.AreEqual(1, uri["_sep"]!.GetValue<int>());

        using var linux = new TestHost(HostOs.Linux);
        var unix = VsCodeTrust.CreateUri("/home/jane/My App", linux.Environment);

        Assert.AreEqual("/home/jane/My App", unix["path"]!.GetValue<string>());
        Assert.AreEqual("file:///home/jane/My%20App", unix["external"]!.GetValue<string>());
        Assert.IsNull(unix["_sep"]);
    }

    private static string ProjectFolder(TestHost host)
    {
        return Directory.CreateDirectory(Path.Combine(host.WorkingDirectory, "Contoso.Shop")).FullName;
    }

    private static string CreateStore(TestHost host, string? trustValue)
    {
        var store = VsCodeTrust.SharedStore(host.Environment);
        Directory.CreateDirectory(Path.GetDirectoryName(store)!);

        using var connection = new SqliteConnection($"Data Source={store};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE ItemTable (key TEXT UNIQUE ON CONFLICT REPLACE, value BLOB)";
        command.ExecuteNonQuery();

        if (trustValue is not null)
        {
            command.CommandText = "INSERT INTO ItemTable (key, value) VALUES ($key, $value)";
            command.Parameters.AddWithValue("$key", VsCodeTrust.TrustKey);
            command.Parameters.AddWithValue("$value", trustValue);
            command.ExecuteNonQuery();
        }

        return store;
    }

    private static string? ReadStore(string store)
    {
        using var connection = new SqliteConnection($"Data Source={store};Pooling=False");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT value FROM ItemTable WHERE key = $key";
        command.Parameters.AddWithValue("$key", VsCodeTrust.TrustKey);
        return command.ExecuteScalar() as string;
    }
}
