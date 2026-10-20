using Bit.BbScanner.Demo.Server.Dtos;
using Bit.BbScanner.Demo.Server.Services;

namespace Bit.BbScanner.Tests.Mcp;

/// <summary>
/// InspectBbScannerSymbol on the symbols an application actually meets: one of every symbology, a payload
/// written without its check digit, a symbol rotated or blurred the way a camera frame is, and input the
/// encoders cannot represent at all.
/// </summary>
[TestClass]
public class McpDecodeLabTests
{
    [TestMethod]
    public async Task Without_a_payload_it_decodes_a_sample_qr_code()
    {
        var report = await InspectAsync(new());

        Assert.IsTrue(report.Encoded);
        Assert.AreEqual("QrCode", report.Format);
        Assert.HasCount(2, report.Attempts!);

        foreach (var attempt in report.Attempts!)
        {
            Assert.IsTrue(attempt.Decoded, attempt.Options);
            Assert.AreEqual(true, attempt.Matches, attempt.Options);
            Assert.AreEqual("QrCode", attempt.Format);
            Assert.HasCount(4, attempt.Corners!, "A matrix code reports its four corners.");
            Assert.AreEqual("M", attempt.Metadata!["ErrorCorrectionLevel"]);
        }
    }

    [TestMethod]
    public async Task Every_symbology_the_pipeline_reads_round_trips_its_sample()
    {
        foreach (var format in BbScannerFormatCatalog.Formats.Where(format => format.Active))
        {
            var report = await InspectAsync(new() { ["format"] = format.Name });

            Assert.IsTrue(report.Encoded, $"{format.Name}: {report.Error}");
            Assert.AreEqual(format.SampleText, report.Text, format.Name);

            foreach (var attempt in report.Attempts!)
            {
                Assert.IsTrue(attempt.Decoded, $"{format.Name} ({attempt.Options})");
                Assert.AreEqual(format.Name, attempt.Format, $"{format.Name} ({attempt.Options})");
                Assert.AreEqual(format.SampleText, attempt.Text, $"{format.Name} ({attempt.Options})");
            }
        }
    }

    [TestMethod]
    public async Task A_missing_check_digit_is_added()
    {
        var report = await InspectAsync(new() { ["format"] = "Ean13", ["text"] = "400638133393" });

        Assert.AreEqual("4006381333931", report.Text);
        Assert.AreEqual("4006381333931", report.Attempts![0].Text);
    }

    [TestMethod]
    public async Task A_rotated_linear_symbol_is_read_by_the_still_image_defaults_and_the_notes_say_why()
    {
        var report = await InspectAsync(new() { ["format"] = "Code128", ["rotation"] = 60 });

        var still = report.Attempts!.Single(attempt => attempt.Options == "still");

        Assert.IsTrue(still.Decoded);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("30 degrees", StringComparison.Ordinal)), string.Join(" | ", report.Notes));
    }

    [TestMethod]
    public async Task A_blurred_symbol_is_still_decoded()
    {
        var report = await InspectAsync(new() { ["format"] = "QrCode", ["text"] = "BLURRED", ["moduleSize"] = 6, ["blur"] = 1.0 });

        Assert.IsTrue(report.Attempts!.Any(attempt => attempt.Decoded));
        Assert.AreEqual(1.0, report.Blur);
    }

    [TestMethod]
    public async Task Pdf417_is_left_unread_while_the_symbol_table_is_a_placeholder()
    {
        var report = await InspectAsync(new() { ["format"] = "Pdf417" });

        Assert.IsTrue(report.Encoded);
        Assert.IsTrue(report.Attempts!.All(attempt => attempt.Decoded is false));
        Assert.IsTrue(report.Notes.Any(note => note.Contains("placeholder", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_payload_the_symbology_cannot_hold_is_reported_rather_than_thrown()
    {
        var report = await InspectAsync(new() { ["format"] = "Ean13", ["text"] = "NOT DIGITS" });

        Assert.IsFalse(report.Encoded);
        Assert.IsNotNull(report.Error);
        Assert.IsNull(report.Attempts);
    }

    [TestMethod]
    public async Task An_unknown_symbology_lists_the_real_ones()
    {
        var report = await InspectAsync(new() { ["format"] = "aztec" });

        Assert.IsFalse(report.Encoded);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("QrCode", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task The_arguments_are_clamped_to_what_the_lab_renders()
    {
        var report = await InspectAsync(new() { ["format"] = "QrCode", ["moduleSize"] = 500, ["blur"] = 99 });

        Assert.AreEqual(12, report.ModuleSize);
        Assert.AreEqual(5, report.Blur);
    }

    [TestMethod]
    public async Task A_frame_too_large_to_render_is_refused_before_anything_is_drawn()
    {
        var started = DateTime.UtcNow;
        var report = await InspectAsync(new()
        {
            ["format"] = "Code128",
            ["text"] = new string('A', 1_000),
            ["moduleSize"] = 12,
            ["rotation"] = 10,
            ["blur"] = 5
        });

        Assert.IsFalse(report.Encoded);
        Assert.IsNull(report.Attempts);
        Assert.Contains("megapixels", report.Error!);
        Assert.IsLessThan(TimeSpan.FromSeconds(5), DateTime.UtcNow - started, "The refusal must not render the frame it refuses.");
    }

    [TestMethod]
    public async Task The_largest_qr_code_still_renders_at_a_module_size_that_fits()
    {
        var report = await InspectAsync(new() { ["format"] = "QrCode", ["text"] = new string('7', 1_000), ["moduleSize"] = 4 });

        Assert.IsTrue(report.Encoded, report.Error);
        Assert.IsTrue(report.Attempts!.Any(attempt => attempt.Decoded));
    }

    internal static Task<BbScannerDecodeReportDto> InspectAsync(Dictionary<string, object?> arguments)
        => McpCall.StructuredAsync<BbScannerDecodeReportDto>("InspectBbScannerSymbol", arguments);
}
