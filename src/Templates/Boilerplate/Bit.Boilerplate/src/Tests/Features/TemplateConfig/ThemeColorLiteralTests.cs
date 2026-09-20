//-:cnd:noEmit
// Conditional processing is off for this whole file, and the marker above has to stay on the very first line.
// This test quotes template.json's own generator names and color literals; with processing on, the engine reads
// such quotes as real directives.

using Bit.BlazorUI;
using System.Text.Json;

namespace Boilerplate.Tests.Features.TemplateConfig;

/// <summary>
/// The theme colors that survive only as literals, and the one thing that keeps them honest.
/// <para>
/// Every head but two reads its background off <c>AppThemePresets</c>, which reads it off bit BlazorUI. The static
/// host pages cannot: no C# runs in an <c>index.html</c>, so their <c>theme-color</c> metas carry a hard-coded
/// <c>#RRGGBB</c>, and <c>template.json</c>'s <c>themeDarkBackground</c> switch carries another one per design
/// system to rewrite it with. bit BlazorUI owns the real values - <c>BitExtraThemeSurfaces.BackgroundPrimary</c>,
/// pinned to the packaged stylesheets by its own contract test - and nothing connects the two.
/// </para>
/// <para>
/// So a palette change in bit BlazorUI leaves these literals behind silently: the app still paints its native
/// chrome correctly, and the installed PWA's status bar, plus the browser chrome of every generated project, keeps
/// the old color and meets the page at a seam. No build and no other test sees it.
/// </para>
/// <para>
/// Like <see cref="TemplateConfigurationTests"/>, this runs against the template's own working copy and reports
/// inconclusive in a generated project (which has no <c>.template.config</c> directory).
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ThemeColorLiteralTests
{
    /// <summary>The host pages with no C# in them, which is why their colors are literals in the first place.</summary>
    private static readonly string[] staticHostPages =
    [
        "src/Client/Boilerplate.Client.Web/wwwroot/index.html",
        "src/Client/Boilerplate.Client.Maui/wwwroot/index.html"
    ];

    private static readonly Regex themeColorMeta = new(
        @"<meta\s+name=""theme-color""\s+content=""(?<color>#[0-9A-Fa-f]{6})""\s+media=""\(prefers-color-scheme:\s*(?<scheme>light|dark)\)""",
        RegexOptions.Compiled);

    private static readonly Regex hexColor = new("#[0-9A-Fa-f]{6}", RegexOptions.Compiled);

    [TestMethod]
    public void EveryHardCodedThemeColor_Should_MatchTheBackgroundBitBlazorUiPaints()
    {
        var (templateRoot, template) = LoadTemplateJson();

        using (template)
        {
            var symbols = template.RootElement.GetProperty("symbols");

            var lightPresets = SwitchCases(symbols, "themeLightName");
            var darkPresets = SwitchCases(symbols, "themeDarkName");
            var darkColors = SwitchCases(symbols, "themeDarkBackground");
            var darkColorTarget = symbols.GetProperty("themeDarkBackground").GetProperty("replaces").GetString()!;

            var unpaired = darkPresets.Keys.Except(darkColors.Keys)
                                      .Concat(darkColors.Keys.Except(darkPresets.Keys))
                                      .ToArray();

            Assert.IsEmpty(unpaired,
                $"themeDarkName and themeDarkBackground no longer switch on the same conditions, so at least one " +
                $"design system takes its preset from one of them and its first-paint color from another. " +
                $"Unpaired: {string.Join(", ", unpaired)}");

            foreach (var (condition, darkPreset) in darkPresets)
            {
                var lightPreset = lightPresets[condition];
                var choice = condition.Length is 0 ? "the default design system" : condition;

                Assert.Contains(lightPreset, BitExtraThemeSurfaces.BackgroundPrimary.Keys,
                    $"template.json pins {choice} to the preset '{lightPreset}', which bit BlazorUI no longer " +
                    $"registers. Every project created with it gets a bit-theme-light attribute naming nothing.");

                Assert.Contains(darkPreset, BitExtraThemeSurfaces.BackgroundPrimary.Keys,
                    $"template.json pins {choice} to the preset '{darkPreset}', which bit BlazorUI no longer " +
                    $"registers. Every project created with it gets a bit-theme-dark attribute naming nothing.");

                Assert.AreEqual(BitExtraThemeSurfaces.BackgroundPrimary[darkPreset], HexOf(darkColors[condition]), ignoreCase: true,
                    $"themeDarkBackground writes {HexOf(darkColors[condition])} for {choice}, while bit BlazorUI " +
                    $"paints '{darkPreset}' with {BitExtraThemeSurfaces.BackgroundPrimary[darkPreset]}. Update the " +
                    $"case to the value bit BlazorUI paints - this literal is all the static host pages have.");
            }

            foreach (var page in staticHostPages)
            {
                var html = File.ReadAllText(Path.Combine(templateRoot, page.Replace('/', Path.DirectorySeparatorChar)));

                Assert.Contains(darkColorTarget, html,
                    $"{page} no longer contains the text themeDarkBackground replaces, so the generator rewrites " +
                    $"nothing and every project created on a design system other than the default one ships the " +
                    $"default one's dark color. Either restore the text or retarget the generator.");

                var metas = themeColorMeta.Matches(html)
                                          .ToDictionary(meta => meta.Groups["scheme"].Value, meta => meta.Groups["color"].Value);

                Assert.HasCount(2, metas,
                    $"{page} should carry one theme-color meta per color scheme, written as a literal this test can " +
                    $"read. Found: {(metas.Count is 0 ? "none" : string.Join(", ", metas.Select(m => $"{m.Key} {m.Value}")))}.");

                // Nothing rewrites the light literal, because every preset the template can pick is white there. The
                // day one is not, it needs a themeLightBackground generator next to the dark one.
                foreach (var (condition, lightPreset) in lightPresets)
                {
                    Assert.AreEqual(BitExtraThemeSurfaces.BackgroundPrimary[lightPreset], metas["light"], ignoreCase: true,
                        $"{page} paints its light theme-color {metas["light"]}, while bit BlazorUI paints " +
                        $"'{lightPreset}' with {BitExtraThemeSurfaces.BackgroundPrimary[lightPreset]}. The light " +
                        $"literal is not generated per design system, so it only works while they all agree.");
                }

                Assert.AreEqual(HexOf(darkColors[string.Empty]), metas["dark"], ignoreCase: true,
                    $"{page} paints its dark theme-color {metas["dark"]}, which is not what themeDarkBackground's " +
                    $"default case writes. A project created without --theme is the one generation that never " +
                    $"reaches that case, so the page's own literal is what it ships with.");
            }
        }
    }

    /// <summary>A switch generator's cases, keyed by condition - the default case's condition is the empty string.</summary>
    private static Dictionary<string, string> SwitchCases(JsonElement symbols, string symbolName)
    {
        Assert.IsTrue(symbols.TryGetProperty(symbolName, out var symbol),
            $"template.json has no '{symbolName}' symbol. If the theme literals are generated some other way now, " +
            $"point this test at it rather than deleting the assertion.");

        return symbol.GetProperty("parameters")
                     .GetProperty("cases")
                     .EnumerateArray()
                     .ToDictionary(item => item.GetProperty("condition").GetString()!,
                                   item => item.GetProperty("value").GetString()!);
    }

    /// <summary>The color out of a generated value, which is a whole attribute rather than the color alone.</summary>
    private static string HexOf(string generatedValue)
    {
        var color = hexColor.Match(generatedValue);

        Assert.IsTrue(color.Success, $"'{generatedValue}' carries no #RRGGBB color for this test to compare.");

        return color.Value;
    }

    /// <summary>
    /// Finds the template root by walking up from the test binaries. <c>template.json</c> carries <c>//</c> comments,
    /// so it is not strict JSON.
    /// </summary>
    private static (string TemplateRoot, JsonDocument Template) LoadTemplateJson()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && File.Exists(Path.Combine(directory.FullName, ".template.config", "template.json")) is false)
        {
            directory = directory.Parent;
        }

        if (directory is null)
        {
            Assert.Inconclusive("No .template.config/template.json above the test binaries - this is a generated project, not the template's own tree.");
            return default;
        }

        var template = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(directory.FullName, ".template.config", "template.json")),
            new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

        return (directory.FullName, template);
    }
}
