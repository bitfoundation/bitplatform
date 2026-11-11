using System.Text.Json;
using Bit.Bheadless.Tests.Mcp.TestInfra;

namespace Bit.Bheadless.Tests.Mcp;

/// <summary>
/// The server as an agent meets it - the real Program.cs hosted in memory, spoken to over the MCP HTTP
/// transport - and as a browser meets it, through the GET mirror of every tool. A tool that is not
/// decorated is not registered, an argument name that differs from the parameter's arrives as null, and
/// neither shows up in a method call; both show up here.
/// </summary>
[TestClass]
public class McpServerTests
{
    private static McpServerFixture? _fixture;

    private static McpServerFixture Server
        => _fixture ?? throw new InvalidOperationException("The in-memory MCP server did not start; see ClassInitialize.");

    [ClassInitialize]
    public static async Task StartServerAsync(TestContext _) => _fixture = await McpServerFixture.StartAsync();

    [ClassCleanup]
    public static async Task StopServerAsync()
    {
        if (_fixture is { } fixture)
        {
            _fixture = null;
            await fixture.DisposeAsync();
        }
    }

    /// <summary>The tool count is the design - every description is paid for in every request - so a fifth is a decision, not a drift.</summary>
    [TestMethod]
    public async Task ListTools_ExposesTheFourTools_EachWithADescriptionAndAnInputSchema()
    {
        var tools = await Server.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);

        Assert.AreSequenceEqual(
            new[]
            {
                nameof(McpController.SearchBheadless),
                nameof(McpController.GetBheadlessComponent),
                nameof(McpController.GetBheadlessExamples),
                nameof(McpController.GetBheadlessSetupGuide),
            }, tools.Select(tool => tool.Name).ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);

        foreach (var tool in tools)
        {
            var actualValue = tool.Description?.Length > 60;
            Assert.IsTrue(actualValue, $"'{tool.Name}' has too short a description to be chosen by.");
            Assert.AreEqual(JsonValueKind.Object, tool.ProtocolTool.InputSchema.ValueKind, $"'{tool.Name}' has no input schema.");

            // Markdown answers, never a structured payload sent twice.
            Assert.IsNull(tool.ProtocolTool.OutputSchema, $"'{tool.Name}' publishes an output schema.");
        }

        Assert.IsFalse(string.IsNullOrWhiteSpace(Server.Client.ServerInstructions), "The server gives no instructions.");
    }

    [TestMethod]
    public async Task Catalog_ListsEveryDemoCatalogSlug_OverMcpAndOverGet()
    {
        foreach (var catalog in new[]
        {
            await Server.CallAsync(nameof(McpController.GetBheadlessComponent)),
            await Server.GetAsync(nameof(McpController.GetBheadlessComponent)),
        })
        {
            foreach (var entry in DemoCatalog.Components)
            {
                Assert.Contains($"(`{entry.Slug}`)", catalog, $"The catalog leaves out '{entry.Slug}'.");
                Assert.Contains($"## {DemoCatalog.AreaLabel(entry.Area)}", catalog);
            }
        }
    }

    [TestMethod]
    public async Task Component_NamesItsPartsAndTheirParameters()
    {
        var menu = await Server.CallAsync(nameof(McpController.GetBheadlessComponent), new() { ["names"] = "Menu" });

        foreach (var part in new[] { "## BitBhMenu", "## BitBhMenuButton", "## BitBhMenuItems", "## BitBhMenuItem", "## BitBhMenuSubTrigger" })
        {
            Assert.Contains(part, menu);
        }

        Assert.Contains("| `Open` | `bool` | `false` |", menu);
        Assert.Contains("`@bind-Open`", menu);
        Assert.Contains("data-highlighted", menu);
        // The common parameters are named once, not per part.
        Assert.AreEqual(1, menu.Split("| `As` |").Length - 1, "BitBhComponentBase's As is repeated.");
        Assert.Contains("```razor", menu);

        // A part answers with its whole family, the same over the GET mirror.
        var item = await Server.GetAsync(nameof(McpController.GetBheadlessComponent), "names=BitBhMenuItem");

        Assert.Contains("# Menu (`menu`", item);
        Assert.Contains("_`BitBhMenuItem` is one of this family's parts", item);
    }

    [TestMethod]
    public async Task Component_AMiss_SuggestsTheNearestNames_RatherThanFailing()
    {
        var answer = await Server.CallAsync(nameof(McpController.GetBheadlessComponent), new() { ["names"] = "BitBhMenuItm" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("BitBhMenuItem", answer);

        var overGet = await Server.GetAsync(nameof(McpController.GetBheadlessComponent), "names=Accordian");

        Assert.Contains("accordion", overGet);
    }

    [TestMethod]
    public async Task Search_FindsAPart_ByOneOfItsParameterNames()
    {
        // HiddenUntilFound is a parameter of three content parts and the name of none of them.
        var answer = await Server.CallAsync(nameof(McpController.SearchBheadless), new() { ["query"] = "HiddenUntilFound" });

        Assert.Contains("BitBhAccordionContent.HiddenUntilFound", answer);
        Assert.Contains("GetBheadlessComponent(names: \"BitBhAccordionContent\")", answer);

        var overGet = await Server.GetAsync(nameof(McpController.SearchBheadless), "query=DismissOnEscape&limit=3");

        Assert.Contains("GetBheadlessComponent(names: \"BitBhDialog\")", overGet);
    }

    [TestMethod]
    public async Task Examples_ReturnTheRazorAndCssTheDemoPageRuns()
    {
        var css = await Server.CallAsync(nameof(McpController.GetBheadlessExamples), new() { ["name"] = "accordion", ["example"] = "Multiple" });

        Assert.Contains("## Multiple Open Items", css);
        Assert.Contains("<BitBhAccordion Type=\"BitBhAccordionType.Multiple\"", css);
        Assert.Contains("```css", css);

        var tailwind = await Server.GetAsync(nameof(McpController.GetBheadlessExamples), "name=BitBhAccordionTrigger&example=Multiple&styling=tailwind");

        Assert.Contains("data-state-open:", tailwind);
        Assert.Contains("@custom-variant data-state-open", tailwind);
    }

    [TestMethod]
    public async Task SetupGuide_WithoutATopic_ReturnsTheSectionsAndTheInstallationSection()
    {
        if (BheadlessGuide.Readme is null)
        {
            Assert.Inconclusive("The library README was not embedded in this build (src/Utilities/Bheadless/README.md is missing).");
        }

        foreach (var guide in new[]
        {
            await Server.CallAsync(nameof(McpController.GetBheadlessSetupGuide)),
            await Server.GetAsync(nameof(McpController.GetBheadlessSetupGuide)),
        })
        {
            Assert.Contains("## Sections of the guide", guide);
            Assert.Contains("## Installation", guide);
            Assert.Contains("AddBitBheadlessServices()", guide);
        }
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
