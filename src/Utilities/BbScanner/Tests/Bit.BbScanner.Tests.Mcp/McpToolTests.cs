using Bit.BbScanner.Demo.Server.Services;

namespace Bit.BbScanner.Tests.Mcp;

/// <summary>
/// Every reference tool's answers over the protocol: the thing a key names, the index a missing key
/// answers with, and the guidance a wrong key gets instead of an error.
/// </summary>
[TestClass]
public class McpToolTests
{
    // ---- Guide -------------------------------------------------------------

    [TestMethod]
    public async Task The_guide_index_lists_every_top_level_section()
    {
        var index = await McpCall.TextAsync("GetBbScannerGuideSection");

        foreach (var heading in new[] { "Getting started", "Symbologies", "Using the component", "Styling", "Using the decoder without Blazor", "Hosting models", "Errors and permissions", "What it reads, and what it does not", "Performance", "Architecture", "PDF417 status" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBbScannerGuideSection", new() { ["heading"] = "ERRORS & permissions" });

        Assert.StartsWith("## Errors and permissions", section);
        Assert.Contains("PermissionDenied", section);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBbScannerGuideSection", new() { ["heading"] = "Getting started" });

        Assert.Contains("### A first scanner", section);
        Assert.DoesNotContain("## Symbologies", section);
    }

    [TestMethod]
    public async Task Every_heading_of_the_guide_has_a_key_of_its_own()
    {
        foreach (var section in BbScannerSourceCatalog.GuideSections)
        {
            var text = await McpCall.TextAsync("GetBbScannerGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBbScannerGuideSection", new() { ["heading"] = "decoder" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Using the decoder without Blazor", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_and_not_the_internal_ones()
    {
        var index = await McpCall.TextAsync("GetBbScannerApi");

        foreach (var type in new[] { "BbScanner", "BbScannerOptions", "BbBarcodeFormat", "BbBarcodeResult", "BbBarcodeDecoder", "BbScanRegion", "BbScannerException", "IBbRowDecoder" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference.
        Assert.DoesNotContain("BbScannerJsInterop", index);
        Assert.DoesNotContain("BbScannerJsonContext", index);
        Assert.DoesNotContain("BbDetectorScratch", index);
    }

    [TestMethod]
    public async Task A_component_reference_lists_its_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBbScannerApi", new() { ["typeName"] = "BbScanner" });

        Assert.StartsWith("# BbScanner (Component)", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**Formats** : `BbBarcodeFormat` = `BbBarcodeFormat.All`", reference);
        Assert.Contains("**TargetFrameRate** : `int` = `15`", reference);
        Assert.Contains("**Facing** : `BbCameraFacing` = `BbCameraFacing.Rear`", reference);
        Assert.Contains("**OnScan** : `EventCallback<BbBarcodeResult>`", reference);
    }

    [TestMethod]
    public async Task Option_defaults_are_read_off_a_real_instance()
    {
        var reference = await McpCall.TextAsync("GetBbScannerApi", new() { ["typeName"] = "BbScannerOptions" });

        Assert.Contains("**MaxScanLines** : `int` = `15`", reference);
        Assert.Contains("**TryReversedRows** : `bool` = `true`", reference);
        Assert.Contains("**TryHarder** : `bool` = `false`", reference);
        Assert.Contains("**Region** : `BbScanRegion` = `BbScanRegion.Full`", reference);
    }

    [TestMethod]
    public async Task A_struct_left_at_its_default_says_so_in_one_word()
    {
        var reference = await McpCall.TextAsync("GetBbScannerApi", new() { ["typeName"] = "BbScanner" });

        var line = reference.Split('\n').Single(line => line.StartsWith("- **Diagnostics**", StringComparison.Ordinal));

        Assert.Contains("= `default`", line);
    }

    [TestMethod]
    public async Task What_the_compiler_writes_for_a_record_is_left_out()
    {
        var reference = await McpCall.TextAsync("GetBbScannerApi", new() { ["typeName"] = "BbCameraDevice" });

        Assert.DoesNotContain("- **Equals**", reference);
        Assert.DoesNotContain("- **GetHashCode**", reference);
        Assert.DoesNotContain("- **ToString**", reference);
        Assert.DoesNotContain("<Clone>", reference);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBbScannerApi", new() { ["typeName"] = "ScannerOptions" });

        Assert.Contains("BbScannerOptions", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBbScannerApi", new() { ["typeName"] = "Zebra" });

        Assert.Contains("prefixed 'Bb'", answer);
    }

    // ---- Formats -----------------------------------------------------------

    [TestMethod]
    public async Task The_format_table_lists_every_symbology()
    {
        var table = await McpCall.TextAsync("GetBbScannerFormats");

        foreach (var format in BbScannerFormatCatalog.Formats) Assert.Contains($"| `{format.Name}` |", table);
    }

    [TestMethod]
    public async Task A_format_comes_with_its_status_and_the_options_that_steer_it()
    {
        var qr = await McpCall.TextAsync("GetBbScannerFormats", new() { ["format"] = "QrCode" });

        Assert.StartsWith("# QR Code (`BbBarcodeFormat.QrCode`)", qr);
        Assert.Contains("ISO/IEC 18004", qr);
        Assert.Contains("| `TryHarder` | `bool` | `false` |", qr);
        Assert.Contains("| `AllowInverted` | `bool` | `false` |", qr);

        var itf = await McpCall.TextAsync("GetBbScannerFormats", new() { ["format"] = "Itf" });

        Assert.Contains("| `ItfLengths` |", itf);
        Assert.Contains("[6, 8, 10, 12, 14, 16, 18, 20]", itf);
    }

    [TestMethod]
    public async Task A_format_is_found_by_its_usual_name_too()
    {
        var byName = await McpCall.TextAsync("GetBbScannerFormats", new() { ["format"] = "EAN-13" });
        var byMember = await McpCall.TextAsync("GetBbScannerFormats", new() { ["format"] = "Ean13" });

        Assert.StartsWith("# EAN-13 (`BbBarcodeFormat.Ean13`)", byName);
        Assert.AreEqual(byMember, byName);
    }

    [TestMethod]
    public async Task Pdf417_says_why_the_pipeline_does_not_read_it()
    {
        var pdf417 = await McpCall.TextAsync("GetBbScannerFormats", new() { ["format"] = "Pdf417" });

        Assert.Contains("The pipeline does not create this reader", pdf417);
        Assert.Contains("BbPdf417SymbolTable.Register", pdf417);
    }

    [TestMethod]
    public async Task An_unknown_format_lists_the_real_ones()
    {
        var answer = await McpCall.TextAsync("GetBbScannerFormats", new() { ["format"] = "aztec" });

        Assert.Contains("no symbology called 'aztec'", answer);
        foreach (var format in BbScannerFormatCatalog.Formats) Assert.Contains(format.Name, answer);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_render_mode_gets_the_checklist_and_the_guide_on_its_hosting_model()
    {
        foreach (var mode in BbScannerSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBbScannerSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("dotnet add package Bit.BbScanner", guide, mode);
            Assert.Contains("@using Bit.BbScanner", guide, mode);
            Assert.Contains("NO services to register", guide, mode);
            Assert.Contains("## From the guide", guide, mode);
            Assert.DoesNotContain("AddBitBbScanner", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_server_mode_explains_that_the_camera_needs_webassembly()
    {
        var guide = await McpCall.TextAsync("GetBbScannerSetupGuide", new() { ["renderMode"] = "server" });

        Assert.Contains("NotSupported", guide);
        Assert.Contains("BbBarcodeDecoder", guide);
        Assert.Contains("#### Blazor Server", guide, "The quoted guide section should be demoted under 'From the guide'.");
    }

    [TestMethod]
    public async Task The_webassembly_mode_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBbScannerSetupGuide", new() { ["renderMode"] = "wasm" });

        Assert.Contains(BbScannerSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Components/App.razor`", guide);
    }

    [TestMethod]
    public async Task An_unknown_render_mode_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past
        // it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBbScannerSetupGuide", new() { ["renderMode"] = "electron" });

        Assert.Contains("'electron' is not a known render mode", answer);
        foreach (var mode in BbScannerSetupGuide.RenderModes) Assert.Contains(mode, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_demo_pages_and_the_stylesheet_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBbScannerSourceFile");

        Assert.Contains("Demo/Client/Pages/Scan.razor", index);
        Assert.Contains("Demo/Client/Pages/Camera.razor", index);
        Assert.Contains(BbScannerSourceCatalog.StylesheetPath, index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBbScannerSourceFile", new() { ["path"] = "Demo/Client/Pages/Scan.razor" });

        Assert.Contains("<BbScanner", file);
        Assert.Contains("@page \"/scan\"", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBbScannerSourceFile", new() { ["path"] = "Camera" });

        Assert.Contains("Demo/Client/Pages/Camera.razor", answer);
    }
}
