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
        var tools = await Server.Client.ListToolsAsync();

        CollectionAssert.AreEquivalent(
            new[]
            {
                nameof(McpController.SearchBheadless),
                nameof(McpController.GetBheadlessComponent),
                nameof(McpController.GetBheadlessExamples),
                nameof(McpController.GetBheadlessGuide),
            },
            tools.Select(tool => tool.Name).ToArray());

        foreach (var tool in tools)
        {
            Assert.IsTrue(tool.Description?.Length > 60, $"'{tool.Name}' has too short a description to be chosen by.");
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
                StringAssert.Contains(catalog, $"(`{entry.Slug}`)", $"The catalog leaves out '{entry.Slug}'.");
                StringAssert.Contains(catalog, $"## {DemoCatalog.AreaLabel(entry.Area)}");
            }
        }
    }

    [TestMethod]
    public async Task Component_NamesItsPartsAndTheirParameters()
    {
        var menu = await Server.CallAsync(nameof(McpController.GetBheadlessComponent), new() { ["names"] = "Menu" });

        foreach (var part in new[] { "## BitBhMenu", "## BitBhMenuButton", "## BitBhMenuItems", "## BitBhMenuItem", "## BitBhMenuSubTrigger" })
        {
            StringAssert.Contains(menu, part);
        }

        StringAssert.Contains(menu, "| `Open` | `bool` | `false` |");
        StringAssert.Contains(menu, "`@bind-Open`");
        StringAssert.Contains(menu, "data-highlighted");
        // The common parameters are named once, not per part.
        Assert.AreEqual(1, menu.Split("| `As` |").Length - 1, "BitBhComponentBase's As is repeated.");
        StringAssert.Contains(menu, "```razor");

        // A part answers with its whole family, the same over the GET mirror.
        var item = await Server.GetAsync(nameof(McpController.GetBheadlessComponent), "names=BitBhMenuItem");

        StringAssert.Contains(item, "# Menu (`menu`");
        StringAssert.Contains(item, "_`BitBhMenuItem` is one of this family's parts");
    }

    [TestMethod]
    public async Task Component_AMiss_SuggestsTheNearestNames_RatherThanFailing()
    {
        var answer = await Server.CallAsync(nameof(McpController.GetBheadlessComponent), new() { ["names"] = "BitBhMenuItm" });

        StringAssert.Contains(answer, "Did you mean");
        StringAssert.Contains(answer, "BitBhMenuItem");

        var overGet = await Server.GetAsync(nameof(McpController.GetBheadlessComponent), "names=Accordian");

        StringAssert.Contains(overGet, "accordion");
    }

    [TestMethod]
    public async Task Search_FindsAPart_ByOneOfItsParameterNames()
    {
        // HiddenUntilFound is a parameter of three content parts and the name of none of them.
        var answer = await Server.CallAsync(nameof(McpController.SearchBheadless), new() { ["query"] = "HiddenUntilFound" });

        StringAssert.Contains(answer, "BitBhAccordionContent.HiddenUntilFound");
        StringAssert.Contains(answer, "GetBheadlessComponent(names: \"BitBhAccordionContent\")");

        var overGet = await Server.GetAsync(nameof(McpController.SearchBheadless), "query=DismissOnEscape&limit=3");

        StringAssert.Contains(overGet, "GetBheadlessComponent(names: \"BitBhDialog\")");
    }

    [TestMethod]
    public async Task Examples_ReturnTheRazorAndCssTheDemoPageRuns()
    {
        var css = await Server.CallAsync(nameof(McpController.GetBheadlessExamples), new() { ["name"] = "accordion", ["example"] = "Multiple" });

        StringAssert.Contains(css, "## Multiple Open Items");
        StringAssert.Contains(css, "<BitBhAccordion Type=\"BitBhAccordionType.Multiple\"");
        StringAssert.Contains(css, "```css");

        var tailwind = await Server.GetAsync(nameof(McpController.GetBheadlessExamples), "name=BitBhAccordionTrigger&example=Multiple&styling=tailwind");

        StringAssert.Contains(tailwind, "data-state-open:");
        StringAssert.Contains(tailwind, "@custom-variant data-state-open");
    }

    [TestMethod]
    public async Task Guide_WithoutATopic_ReturnsTheSectionsAndTheInstallationSection()
    {
        if (BheadlessGuide.Readme is null)
        {
            Assert.Inconclusive("The library README was not embedded in this build (src/Utilities/Bheadless/README.md is missing).");
        }

        foreach (var guide in new[]
        {
            await Server.CallAsync(nameof(McpController.GetBheadlessGuide)),
            await Server.GetAsync(nameof(McpController.GetBheadlessGuide)),
        })
        {
            StringAssert.Contains(guide, "## Sections of the guide");
            StringAssert.Contains(guide, "## Installation");
            StringAssert.Contains(guide, "AddBitBheadlessServices()");
        }
    }
}
