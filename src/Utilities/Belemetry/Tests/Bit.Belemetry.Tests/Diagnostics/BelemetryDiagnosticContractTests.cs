using System.Text.RegularExpressions;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>The wire contracts of diagnostics shared by C# and TypeScript, checked against the script's source.</summary>
[TestClass]
public class BelemetryDiagnosticContractTests
{
    private static string Script(string file) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Bit.Belemetry", "Scripts", file));

    [TestMethod]
    public void TheCategoryBits_AreTheScriptsCategoryBits()
    {
        // diagnostics.ts: `export const Category = { navigation: 1, ... }` - the names are BelemetryDiagnosticJson's.
        var source = Script("diagnostics.ts");
        var block = Regex.Match(source, @"export const Category = \{(?<body>[^}]+)\}").Groups["body"].Value;
        var bits = Regex.Matches(block, @"'?(?<name>[\w-]+)'?:\s*(?<bit>\d+)").ToDictionary(m => m.Groups["name"].Value, m => int.Parse(m.Groups["bit"].Value));

        foreach (var category in Enum.GetValues<BelemetryDiagnosticCategories>().Where(c => c is not BelemetryDiagnosticCategories.None and not BelemetryDiagnosticCategories.All))
        {
            var name = BelemetryDiagnosticJson.CategoryName(category);
            Assert.IsTrue(bits.TryGetValue(name, out var bit), $"The script has no category '{name}'.");
            Assert.AreEqual((int)category, bit, name);
            Assert.AreEqual(category, BelemetryDiagnosticJson.ParseCategory(name));
        }
        Assert.AreEqual(bits.Count, Enum.GetValues<BelemetryDiagnosticCategories>().Length - 2);
        Assert.Contains($"categories: {(int)BelemetryDiagnosticCategories.All}", Script("config.ts"));
    }

    [TestMethod]
    public void TheScriptsTriggerCallback_IsTheOneDotnetExposes()
    {
        Assert.Contains("diagnosticTrigger: 'Belemetry.DiagnosticTrigger'", Script(Path.Combine("exporters", "relay.ts")));
        var method = typeof(BelemetryJsCallbacks).GetMethod(nameof(BelemetryJsCallbacks.DiagnosticTrigger))!;
        Assert.AreEqual("Belemetry.DiagnosticTrigger", method.GetCustomAttributes(typeof(Microsoft.JSInterop.JSInvokableAttribute), false).Cast<Microsoft.JSInterop.JSInvokableAttribute>().Single().Identifier);
    }

    [TestMethod]
    public void TheConfig_CarriesDiagnostics_OnlyWhenEnabled()
    {
        var off = BelemetryBrowserConfigFactory.ToJson(BelemetryBrowserConfigFactory.Create(TestHost.CreateSettings()));
        Assert.DoesNotContain("diagnostics", off);

        var settings = TestHost.CreateSettings(o =>
        {
            o.Diagnostics.Enabled = true;
            o.Diagnostics.BrowserMaxEvents = 5000;
            o.Diagnostics.PersistBrowserTimeline = true;
            o.Diagnostics.AllowedQueryParameters.Add("page");
            o.Diagnostics.AutoCaptureTriggers = BelemetryDiagnosticTriggers.BrowserErrors;
        });
        using var json = JsonDocument.Parse(BelemetryBrowserConfigFactory.ToJson(BelemetryBrowserConfigFactory.Create(settings)));
        var diagnostics = json.RootElement.GetProperty("diagnostics");

        Assert.IsTrue(diagnostics.GetProperty("enabled").GetBoolean());
        Assert.AreEqual(1000, diagnostics.GetProperty("maxEvents").GetInt32(), "Bounded whatever the configuration says.");
        Assert.AreEqual((int)BelemetryDiagnosticCategories.All, diagnostics.GetProperty("categories").GetInt32());
        Assert.IsTrue(diagnostics.GetProperty("persist").GetBoolean());
        Assert.IsTrue(diagnostics.GetProperty("triggerBrowserErrors").GetBoolean());
        Assert.IsFalse(diagnostics.GetProperty("includeLogMessages").GetBoolean());
        Assert.AreEqual("page", diagnostics.GetProperty("allowedQueryParameters")[0].GetString());

        // Every key the script reads (config.ts DiagnosticsConfig) is written.
        var keys = diagnostics.EnumerateObject().Select(p => p.Name).ToHashSet();
        var block = Regex.Match(Script("config.ts"), @"export interface DiagnosticsConfig \{(?<body>[^}]+)\}").Groups["body"].Value;
        foreach (Match field in Regex.Matches(block, @"^\s+(?<name>\w+):", RegexOptions.Multiline)) Assert.Contains(field.Groups["name"].Value, keys);
    }

    [TestMethod]
    public void TheBrowserEventShape_IsWhatTheReaderReads()
    {
        // diagnostics.ts DiagnosticEvent: the reader must know every field the script writes (and nothing else is trusted).
        var block = Regex.Match(Script("diagnostics.ts"), @"export interface DiagnosticEvent \{(?<body>[^}]+?)\n\}", RegexOptions.Singleline).Groups["body"].Value;
        var fields = Regex.Matches(block, @"^\s+(?<name>\w+)\??:", RegexOptions.Multiline).Select(m => m.Groups["name"].Value).ToArray();
        var reader = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Bit.Belemetry", "Diagnostics", "BelemetryBrowserEvidence.cs"));
        foreach (var field in fields.Except(["type", "message", "stack"]))
        {
            Assert.Contains($"\"{field}\"", reader, $"BelemetryBrowserEvidence does not read '{field}'.");
        }
    }
}
