using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Bit.BbScanner.Demo.Server.Services;

namespace Bit.BbScanner.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the decode lab against the decoder it runs, the
/// symbology catalog against the enum and the options it was read from, the API index against the assembly,
/// the stylesheet classes and custom properties against the stylesheet, and the HTTP mirror against the
/// protocol it mirrors. No expected answers are written down here - the library, its files and the protocol
/// are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    [TestMethod]
    public async Task The_decode_lab_hands_back_the_bytes_that_were_encoded()
    {
        var random = new Random(20261017);
        const string alphabet = "abcdefghijklmnopqrstuvwxyz0123456789-./:";

        for (var i = 0; i < 6; i++)
        {
            // Lower-case letters put the QR encoder in byte mode, where the raw bytes are the UTF-8 of the text.
            var payload = new string([.. Enumerable.Range(0, random.Next(5, 60)).Select(_ => alphabet[random.Next(alphabet.Length)])]) + "x";

            var report = await McpDecodeLabTests.InspectAsync(new() { ["format"] = "QrCode", ["text"] = payload });

            foreach (var attempt in report.Attempts!)
            {
                Assert.AreEqual(payload, attempt.Text, attempt.Options);
                Assert.AreEqual(Convert.ToHexString(Encoding.UTF8.GetBytes(payload)), attempt.RawBytes, attempt.Options);
                Assert.AreEqual(Encoding.UTF8.GetByteCount(payload), attempt.RawByteCount, attempt.Options);
            }
        }
    }

    [TestMethod]
    public void The_format_catalog_is_the_enum_and_the_option_defaults()
    {
        var singles = Enum.GetValues<BbBarcodeFormat>()
            .Where(format => (int)format != 0 && ((int)format & ((int)format - 1)) == 0)
            .Select(format => format.ToString())
            .ToArray();

        CollectionAssert.AreEqual(singles, BbScannerFormatCatalog.Formats.Select(format => format.Name).ToArray());

        var defaults = new BbScannerOptions();

        foreach (var entry in BbScannerFormatCatalog.Formats)
        {
            var format = Enum.Parse<BbBarcodeFormat>(entry.Name);

            Assert.AreEqual((int)format, entry.Value, entry.Name);
            Assert.AreEqual((defaults.Formats & format) != 0, entry.EnabledByDefault, entry.Name);
            Assert.AreEqual((BbBarcodeFormat.AllMatrix & format) != 0 ? "Matrix" : "Linear", entry.Family, entry.Name);
            Assert.AreEqual(format != BbBarcodeFormat.Pdf417 || BbPdf417SymbolTable.Current.IsSpecificationTable, entry.Active, entry.Name);
            Assert.IsNotNull(entry.Status, $"{entry.Name} is missing from the guide's symbology table.");

            foreach (var option in entry.Options)
            {
                var property = typeof(BbScannerOptions).GetProperty(option.Name);

                Assert.IsNotNull(property, $"{entry.Name} lists '{option.Name}', which BbScannerOptions does not have.");
                Assert.AreEqual(BbScannerFormatCatalog.Describe(property.GetValue(defaults)), option.Default, $"{entry.Name}: {option.Name}");
            }
        }
    }

    [TestMethod]
    public void Every_exported_type_is_in_the_api_index()
    {
        var exported = typeof(BbBarcodeDecoder).Assembly.GetExportedTypes()
            .Where(type => type.IsNested is false)
            .Select(type => BbScannerApiCatalog.FriendlyName(type))
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BbScannerApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
    }

    [TestMethod]
    public void Every_documented_public_type_has_its_summary_in_the_index()
    {
        // The XML documentation file has to reach the server's output folder for any of this to work;
        // a build that stops copying it leaves every summary empty without failing anything else.
        var withSummary = BbScannerApiCatalog.Types.Count(type => type.Summary is not null);

        Assert.IsGreaterThan(BbScannerApiCatalog.Types.Length * 9 / 10, withSummary, "Most public types should come with their XML summary.");
    }

    [TestMethod]
    public void Every_class_and_custom_property_of_the_stylesheet_is_a_search_entry()
    {
        var css = BbScannerSourceCatalog.Stylesheet;

        Assert.IsFalse(string.IsNullOrEmpty(css), "The shipped stylesheet is not embedded.");

        var classes = ClassRegex().Matches(css).Select(match => match.Value).Distinct(StringComparer.Ordinal).ToArray();
        var selectors = string.Join(' ', BbScannerStylesheetClasses.Classes.Select(rule => rule.Selector));

        Assert.IsGreaterThan(5, classes.Length);
        foreach (var name in classes) Assert.Contains(name, selectors);

        // Every property the stylesheet declares a default for is a theming knob; the ones it only reads with a
        // fallback (the aspect ratio and the highlight's position) are set inline by the component.
        var declared = DeclaredPropertyRegex().Matches(css).Select(match => match.Groups["name"].Value).Distinct(StringComparer.Ordinal).ToArray();

        Assert.IsGreaterThan(10, declared.Length);
        CollectionAssert.AreEquivalent(declared, BbScannerStylesheetClasses.Properties.Select(property => property.Name).ToArray());
    }

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BbScannerSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
        }

        CollectionAssert.AreEqual(headings, BbScannerSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BbScannerSourceCatalog.GuideSections.Length, BbScannerSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public void The_setup_guides_quote_sections_the_guide_has()
    {
        foreach (var mode in BbScannerSetupGuide.RenderModes)
        {
            var guide = BbScannerSetupGuide.Get(mode)!;
            var quoted = guide[guide.IndexOf("## From the guide", StringComparison.Ordinal)..];

            Assert.IsTrue(quoted.Contains("\n### ", StringComparison.Ordinal) || quoted.Contains("\n#### ", StringComparison.Ordinal),
                $"The '{mode}' setup guide quotes no section of the guide: a heading it names has been renamed.");
        }
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBbScanner", "?query=torch&limit=3", new() { ["query"] = "torch", ["limit"] = 3 }),
            ("GetBbScannerSetupGuide", "?renderMode=server", new() { ["renderMode"] = "server" }),
            ("GetBbScannerGuideSection", "?heading=Symbologies", new() { ["heading"] = "Symbologies" }),
            ("GetBbScannerApi", "?typeName=BbScanner", new() { ["typeName"] = "BbScanner" }),
            ("GetBbScannerFormats", "?format=QrCode", new() { ["format"] = "QrCode" }),
            ("InspectBbScannerSymbol", "", null),
            ("GetBbScannerSourceFile", "?path=Demo/Client/Pages/Scan.razor", new() { ["path"] = "Demo/Client/Pages/Scan.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBbScanner" or "InspectBbScannerSymbol")
            {
                Assert.StartsWith("{", body, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [GeneratedRegex(@"\.bit-bbscanner(?:__[a-z0-9-]+)?(?:--[a-z0-9-]+)?")]
    private static partial Regex ClassRegex();

    [GeneratedRegex(@"(?<name>--bit-bbscanner-[a-z0-9-]+)\s*:")]
    private static partial Regex DeclaredPropertyRegex();

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();
}
