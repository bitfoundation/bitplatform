using System.Net;
using System.Text.RegularExpressions;
using Bit.Bonboarding.Demo.Server.Dtos;
using Bit.Bonboarding.Demo.Server.Services;

namespace Bit.Bonboarding.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the placement tool against the library's
/// own engine fed exactly the way a running tour feeds it, the theme tool against the stylesheet it
/// was read from, and the HTTP mirror against the protocol it mirrors. No expected answers are
/// written down here - the library, the stylesheet and the protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    [TestMethod]
    public async Task The_placement_tool_agrees_with_the_engine_a_running_tour_uses()
    {
        var random = new Random(20261014);

        foreach (var placement in Enum.GetValues<BonboardingPlacement>())
        {
            for (var i = 0; i < 6; i++)
            {
                var target = new BonboardingRect(random.Next(0, 1200), random.Next(0, 760), random.Next(4, 300), random.Next(4, 200));
                var popover = new BonboardingSize(random.Next(120, 500), random.Next(80, 500));
                var rtl = i % 2 == 1;

                var analysis = await McpCall.StructuredAsync<BonboardingPlacementAnalysisDto>("InspectBonboardingPlacement", new()
                {
                    ["targetX"] = target.X, ["targetY"] = target.Y, ["targetWidth"] = target.Width, ["targetHeight"] = target.Height,
                    ["popoverWidth"] = popover.Width, ["popoverHeight"] = popover.Height,
                    ["placement"] = placement.ToString(), ["rightToLeft"] = rtl
                });

                // What BonboardingTourSession.ApplyGeometry builds: the target grown by the spotlight padding.
                var expected = BonboardingPlacementEngine.Place(new BonboardingPlacementRequest
                {
                    Target = target.Inflate(BonboardingDefaults.SpotlightPadding),
                    Popover = popover,
                    Viewport = new BonboardingRect(0, 0, 1280, 800),
                    Preferred = placement,
                    Offset = BonboardingDefaults.Offset,
                    ViewportPadding = BonboardingDefaults.ViewportPadding,
                    ArrowSize = BonboardingDefaults.ArrowSize,
                    CornerRadius = 12,
                    RightToLeft = rtl,
                    HasTarget = true,
                });

                var context = $"{placement} {target} {popover} rtl={rtl}";

                Assert.AreEqual(expected.Side.ToString(), analysis.Side, context);
                Assert.AreEqual(expected.Align.ToString(), analysis.Align, context);
                Assert.AreEqual(expected.Popover, new BonboardingRect(analysis.Popover.X, analysis.Popover.Y, analysis.Popover.Width, analysis.Popover.Height), context);
                Assert.AreEqual(expected.ShowArrow, analysis.ShowArrow, context);
                Assert.AreEqual(expected.ArrowOffset, analysis.ArrowOffset, context);
                Assert.AreEqual(expected.UsedPreferred, analysis.UsedPreferred, context);
                Assert.AreEqual(expected.Overflow, analysis.Overflow, context);
            }
        }
    }

    [TestMethod]
    public void Every_variable_of_the_stylesheet_root_rule_is_in_the_theme_catalog()
    {
        var css = BonboardingSourceCatalog.Stylesheet.Replace("\r\n", "\n", StringComparison.Ordinal);
        var root = css[css.IndexOf("\n.bit-bonboarding-root {", StringComparison.Ordinal)..];
        root = root[..root.IndexOf("\n}", StringComparison.Ordinal)];

        var declared = DeclarationRegex().Matches(root).Select(match => match.Groups[1].Value).ToArray();

        Assert.IsGreaterThan(30, declared.Length);
        CollectionAssert.AreEqual(declared, BonboardingThemeCatalog.Variables.Select(variable => variable.Name).ToArray());
    }

    [TestMethod]
    public void The_dark_overrides_match_the_dark_rule()
    {
        var accent = BonboardingThemeCatalog.Variables.Single(variable => variable.Name == "--bit-bonboarding-accent");
        var overlay = BonboardingThemeCatalog.Variables.Single(variable => variable.Name == "--bit-bonboarding-overlay");
        var font = BonboardingThemeCatalog.Variables.Single(variable => variable.Name == "--bit-bonboarding-font");

        Assert.AreEqual("#2563eb", accent.LightValue);
        Assert.AreEqual("#6ea8fe", accent.DarkValue);
        Assert.IsNull(overlay.DarkValue, "The dark rule repeats the light value, which is not an override.");
        Assert.IsNull(font.DarkValue);
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBonboarding", "?query=spotlight&limit=3", new() { ["query"] = "spotlight", ["limit"] = 3 }),
            ("GetBonboardingSetupGuide", "?renderMode=server", new() { ["renderMode"] = "server" }),
            ("GetBonboardingGuideSection", "?heading=Persistence", new() { ["heading"] = "Persistence" }),
            ("GetBonboardingApi", "?typeName=BonboardingRoot", new() { ["typeName"] = "BonboardingRoot" }),
            ("GetBonboardingThemeVariables", "", null),
            ("InspectBonboardingPlacement", "?targetX=10&targetY=10&targetWidth=40&targetHeight=40", new() { ["targetX"] = 10, ["targetY"] = 10, ["targetWidth"] = 40, ["targetHeight"] = 40 }),
            ("GetBonboardingSourceFile", "?path=Demo/Client/Pages/Home.razor", new() { ["path"] = "Demo/Client/Pages/Home.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBonboarding" or "InspectBonboardingPlacement")
            {
                Assert.StartsWith("{", body, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [GeneratedRegex(@"^\s*(--bit-bonboarding-[a-z0-9-]+)\s*:", RegexOptions.Multiline)]
    private static partial Regex DeclarationRegex();
}
