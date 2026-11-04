using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Bit.Belemetry.Tests.Diagnostics;

/// <summary>A bundle is a sensitive artifact and an untrusted input: what it must never hold, and how a reader treats a bad one.</summary>
[TestClass]
public class BelemetrySupportBundleSecurityTests
{
    [TestMethod]
    public async Task Credentials_Personal_Data_AndPayloads_NeverReachTheBundle()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Privacy.ScrubPatterns = false);
        var logger = host.Factory.CreateLogger("MyApp.Checkout");

        const string jwt = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJqYW5lIn0.c2lnbmF0dXJlc2lnbmF0dXJl";
        logger.LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException($"Auth failed with Bearer {jwt} for jane@example.com, card 4111 1111 1111 1111")),
            "Checkout for {Email} with {Password} failed", "jane@example.com", "hunter2");
        var span = Records.Span();
        span.Status = BelemetryStatusCode.Error;
        span.Attributes.Set(BelemetrySemanticConventions.HttpRequestMethod, "POST");
        span.Attributes.Set(BelemetrySemanticConventions.UrlFull, "https://jane:pw@api.shop.test/pay?access_token=secret-token&email=jane@example.com");
        span.Attributes.Set("http.request.header.cookie", "session=abc");
        span.Attributes.Set("http.request.body", "{\"cvv\":\"123\"}");
        host.Host.Pipeline.Enqueue(span);
        host.Diagnostics.Record("checkout", new Dictionary<string, object?> { ["apiKey"] = "sk-live-123", ["note"] = jwt });

        var capture = await host.Diagnostics.CaptureAsync();
        capture.Confirm();
        var text = AllText(await capture.ExportAsync());

        foreach (var secret in new[] { jwt, "jane@example.com", "hunter2", "4111 1111 1111 1111", "secret-token", "jane:pw", "session=abc", "cvv", "sk-live-123" })
        {
            Assert.DoesNotContain(secret, text, $"'{secret}' leaked into the bundle.");
        }
    }

    [TestMethod]
    public async Task MaliciousValues_AreData_NeverMarkupOrLinks()
    {
        await using var host = DiagnosticsTestHost.Create();
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("<script>alert(1)</script> [click](javascript:alert(1)) `code` | col")), "x");

        var capture = await host.Diagnostics.CaptureAsync(new() { Description = "<img src=x onerror=alert(1)>" });
        var markdown = capture.ToMarkdown();

        // Inside a code span a Markdown viewer shows text literally (CommonMark); everywhere else no markup may survive.
        var outsideCode = System.Text.RegularExpressions.Regex.Replace(markdown, "`[^`\n]*`", "``");
        Assert.DoesNotContain("<script>", outsideCode);
        Assert.DoesNotContain("<img", outsideCode);
        Assert.DoesNotContain("](javascript:", outsideCode);
        Assert.IsFalse(markdown.Split('\n').Any(line => line.Count(c => c == '`') % 2 != 0), "A backtick in a value cannot break out of its code span.");

        capture.Confirm();
        using var zip = new ZipArchive(new MemoryStream(await capture.ExportAsync()));
        var json = new StreamReader(zip.GetEntry("timeline.json")!.Open()).ReadToEnd();
        Assert.DoesNotContain("<script>", json, "JSON is written with HTML-sensitive characters escaped.");
    }

    [TestMethod]
    public async Task AReader_RejectsWhatIsNotABundle()
    {
        Assert.IsFalse((await BelemetrySupportBundle.ReadAsync(new MemoryStream("not a zip"u8.ToArray()))).IsValid);

        var noManifest = await BelemetrySupportBundle.ReadAsync(Zip(("report.json", "{}")));
        Assert.IsFalse(noManifest.IsValid);
        Assert.Contains("no manifest.json", noManifest.Errors.Single());

        var wrongFormat = await BelemetrySupportBundle.ReadAsync(Zip(("manifest.json", """{"format":"other","schemaVersion":1}""")));
        Assert.Contains("format", wrongFormat.Errors.Single());

        var newer = await BelemetrySupportBundle.ReadAsync(Zip(("manifest.json", """{"format":"bit-belemetry-support-bundle","schemaVersion":99}""")));
        Assert.IsFalse(newer.IsValid);
        Assert.Contains("newer than this reader", newer.Errors.Single());

        var malformed = await BelemetrySupportBundle.ReadAsync(Zip(("manifest.json", "{ not json")));
        Assert.IsFalse(malformed.IsValid);
    }

    [TestMethod]
    public async Task ATamperedFile_FailsItsChecksum()
    {
        await using var host = DiagnosticsTestHost.Create();
        host.Factory.CreateLogger("App").LogError(DiagnosticsTestHost.Thrown(() => new InvalidOperationException("e")), "e");
        var capture = await host.Diagnostics.CaptureAsync();
        capture.Confirm();

        var files = Unzip(await capture.ExportAsync());
        files["report.md"] = "# Everything is fine";
        var read = await BelemetrySupportBundle.ReadAsync(Zip(files.Select(p => (p.Key, p.Value)).ToArray()));

        Assert.IsFalse(read.IsValid);
        Assert.IsTrue(read.Errors.Any(e => e.Contains("'report.md' does not match its SHA-256")));
    }

    [TestMethod]
    public async Task PathTraversal_UnknownFiles_AndDecompressionBombs_AreHandled()
    {
        var manifest = """{"format":"bit-belemetry-support-bundle","schemaVersion":1,"files":[]}""";

        var traversal = await BelemetrySupportBundle.ReadAsync(Zip(("manifest.json", manifest), ("../evil.json", "{}"), ("report.json", """{"schemaVersion":1,"title":"t","summary":"s"}""")));
        Assert.IsTrue(traversal.Errors.Any(e => e.Contains("not allowed")));

        var unknown = await BelemetrySupportBundle.ReadAsync(Zip(("manifest.json", manifest), ("payload.exe", "MZ"), ("report.json", """{"schemaVersion":1,"title":"t","summary":"s"}""")));
        Assert.IsTrue(unknown.IsValid, string.Join("; ", unknown.Errors));
        Assert.IsTrue(unknown.Warnings.Single().Contains("payload.exe"));
        Assert.IsFalse(unknown.Files.ContainsKey("payload.exe"));

        // 64 MiB of zeros compresses to a few KiB: the read stops at the entry limit instead of inflating it.
        using var bomb = new MemoryStream();
        using (var zip = new ZipArchive(bomb, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var entry = zip.CreateEntry("timeline.json", CompressionLevel.SmallestSize).Open())
            {
                var zeros = new byte[1024 * 1024];
                for (var i = 0; i < 64; i++) entry.Write(zeros);
            }
            using var m = zip.CreateEntry("manifest.json").Open();
            m.Write(Encoding.UTF8.GetBytes(manifest));
        }
        bomb.Position = 0;
        var read = await BelemetrySupportBundle.ReadAsync(bomb);
        Assert.IsFalse(read.IsValid);
        Assert.IsLessThan(1024 * 1024, bomb.Length, "The archive itself is small.");
    }

    [TestMethod]
    public async Task UntrustedBrowserEvidence_IsBoundedAndSanitizedAgain()
    {
        await using var host = DiagnosticsTestHost.Create(o => o.Diagnostics.BrowserMaxEvents = 5);
        const string one = """{"seq":0,"time":1700000000000,"cat":"http","name":"GET <b>","sev":17,"outcome":"failure","traceId":"NOT-HEX","feature":16,"attrs":{"url.template":"https://api.test/u/jane@example.com/orders/42?token=x","http.request.header.authorization":"Bearer abc","target.text":"secret"}}""";
        var payload = """{"v":1,"events":[""" + string.Join(',', Enumerable.Repeat(one, 50)) + """],"resource":{"browser.language":"en-US","user_agent.original":"UA"},"dropped":{"capacity":7}}""";
        using var json = JsonDocument.Parse(payload);

        var result = BelemetryBrowserEvidence.Read(json.RootElement, host.Hub);

        Assert.HasCount(10, result.Events, "Bounded to twice what an honest page keeps.");
        Assert.AreEqual(40, result.Dropped["browser.malformed"]);
        Assert.AreEqual(7, result.Dropped["browser.capacity"]);
        var e = result.Events[0];
        Assert.IsNull(e.TraceId, "Not a trace id: not kept.");
        Assert.AreEqual("https://api.test/u/{value}/orders/{n}", e.Attributes["url.template"]);
        Assert.IsFalse(e.Attributes.ContainsKey("http.request.header.authorization"));
        Assert.IsFalse(e.Attributes.ContainsKey("target.text"));
        Assert.AreEqual("en-US", result.Browser["browser.language"]);
        Assert.IsFalse(result.Browser.ContainsKey("user_agent.original"));
    }

    private static string AllText(byte[] bundle) => string.Join('\n', Unzip(bundle).Values);

    private static Dictionary<string, string> Unzip(byte[] bundle)
    {
        using var zip = new ZipArchive(new MemoryStream(bundle));
        return zip.Entries.ToDictionary(e => e.FullName, e => new StreamReader(e.Open()).ReadToEnd());
    }

    private static MemoryStream Zip(params (string Name, string Content)[] files)
    {
        var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (name, content) in files)
            {
                using var entry = zip.CreateEntry(name).Open();
                entry.Write(Encoding.UTF8.GetBytes(content));
            }
        }
        stream.Position = 0;
        return stream;
    }
}
