using Bit.Blex.Demo.Client;
using Bit.Blex.Demo.Server.Services;

namespace Bit.Blex.Tests.Mcp;

/// <summary>
/// GetBlexDiagnostics: the generator's own descriptors joined with the site's explanation of each one.
/// The join is the risk - a diagnostic added to the generator and not to the docs, or documented under
/// the wrong severity, leaves both halves individually fine.
/// </summary>
[TestClass]
public class McpDiagnosticsToolTests
{
    [TestMethod]
    public void The_docs_explain_exactly_the_diagnostics_the_generator_reports()
    {
        var generator = BlexDiagnosticsReference.Descriptors.Select(descriptor => descriptor.Id).ToArray();
        var documented = DiagnosticsCatalog.All.Select(doc => doc.Code).ToArray();

        Assert.IsGreaterThanOrEqualTo(19, generator.Length, $"Only {generator.Length} descriptors were read off the generator.");
        Assert.AreSequenceEqual(generator, documented, global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder, "The diagnostics page and the generator disagree about which diagnostics exist.");
    }

    [TestMethod]
    public void The_docs_give_every_diagnostic_the_severity_and_title_the_generator_reports()
    {
        foreach (var entry in BlexDiagnosticsReference.Entries)
        {
            Assert.IsNotNull(entry.Doc, $"{entry.Id} is not documented.");
            Assert.AreEqual(entry.Severity == "Error", entry.Doc.IsError, $"{entry.Id} is documented as the wrong severity.");
            Assert.AreEqual(entry.Title, entry.Doc.Title, $"{entry.Id} is documented under another title than the one a build reports.");
        }
    }

    [TestMethod]
    public async Task The_index_is_one_table_of_every_diagnostic()
    {
        var index = await McpCall.TextAsync("GetBlexDiagnostics");

        Assert.Contains("| Id | Severity | Title |", index);

        foreach (var entry in BlexDiagnosticsReference.Entries)
        {
            Assert.Contains($"| {entry.Id} | {entry.Severity} |", index);
        }
    }

    [TestMethod]
    public async Task One_diagnostic_comes_with_its_cause_and_both_samples()
    {
        var text = await McpCall.TextAsync("GetBlexDiagnostics", new() { ["code"] = "BLEX001" });

        Assert.StartsWith("# BLEX001 - Store class must be partial", text);
        Assert.Contains("## Rejected", text);
        Assert.Contains("## Fix", text);
        Assert.Contains("public partial class CounterStore", text);
        Assert.IsFalse(text.Contains("<code>", StringComparison.Ordinal), "The cause was handed out as HTML.");
    }

    [TestMethod]
    [DataRow("BLEX009")]
    [DataRow("blex9")]
    [DataRow("9")]
    [DataRow("BLEX0009")]
    public async Task A_code_is_found_however_it_is_spelled(string code)
    {
        Assert.StartsWith("# BLEX009", await McpCall.TextAsync("GetBlexDiagnostics", new() { ["code"] = code }));
    }

    [TestMethod]
    public async Task A_warning_says_how_to_suppress_it()
    {
        Assert.Contains("<NoWarn>$(NoWarn);BLEX011</NoWarn>", await McpCall.TextAsync("GetBlexDiagnostics", new() { ["code"] = "BLEX011" }));
    }

    [TestMethod]
    public async Task An_unknown_code_lists_the_real_ones()
    {
        Assert.Contains("BLEX001", await McpCall.TextAsync("GetBlexDiagnostics", new() { ["code"] = "BLEX999" }));
    }
}
