using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bit.BlazorUI;

if (args.Length < 3 || !int.TryParse(args[0], out int iterations) || iterations <= 0
    || !int.TryParse(args[1], out int warmups) || warmups < 0)
{
    Console.Error.WriteLine("Usage: Bit.BlazorUI.Tests.PdfPerformance <iterations> <warmups> <pdf-path> [pdf-path ...]");
    return 1;
}

string? affinity = Environment.GetEnvironmentVariable("PDF_BENCH_AFFINITY");
if (affinity is not null)
{
    if (OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
    {
        using var process = Process.GetCurrentProcess();
        process.ProcessorAffinity = new IntPtr(long.Parse(affinity));
    }
    else
    {
        throw new PlatformNotSupportedException("PDF_BENCH_AFFINITY requires Windows or Linux.");
    }
}

var results = new List<object>();
foreach (string path in args.Skip(2))
{
    // File transfer and initial buffer allocation are outside these engine timings.
    byte[] bytes = File.ReadAllBytes(path);
    BitPdfDocument reference = BitPdfDocument.Load(bytes);
    int count = reference.PageCount;
    if (count == 0) throw new InvalidOperationException($"Fixture '{Path.GetFileName(path)}' has no pages.");
    string html = new BitPdfHtmlRenderer(reference.Pages[0], reference.XRef).Render();
    var canvas = new BitPdfCanvasRenderer(reference.Pages[0], reference.XRef).Render();
    var metrics = new Dictionary<string, object>
    {
        ["parse"] = Measure(() => BitPdfDocument.Load(bytes)),
        ["parseAndPageTree"] = Measure(() => { var doc = BitPdfDocument.Load(bytes); return doc.PageCount; }),
        ["firstPageHtml"] = Measure(() => new BitPdfHtmlRenderer(reference.Pages[0], reference.XRef).Render()),
        ["firstPageCanvas"] = Measure(() => new BitPdfCanvasRenderer(reference.Pages[0], reference.XRef).Render()),
        ["loadAndFirstPageHtml"] = Measure(() => { var doc = BitPdfDocument.Load(bytes); return new BitPdfHtmlRenderer(doc.Pages[0], doc.XRef).Render(); }),
        ["loadAndFirstPageCanvas"] = Measure(() => { var doc = BitPdfDocument.Load(bytes); return new BitPdfCanvasRenderer(doc.Pages[0], doc.XRef).Render(); }),
    };
    results.Add(new
    {
        fixture = Path.GetFileName(path),
        bytes = bytes.Length,
        sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
        pages = count,
        htmlSha256 = Hash(html),
        canvasSha256 = Hash(JsonSerializer.Serialize(canvas)),
        warnings = reference.Warnings.Count,
        metrics,
    });
}

byte[] hotBytes = new byte[8 * 1024 * 1024];
new Random(13702).NextBytes(hotBytes);
// Direct array-backed reads provide a control for timing noise.
var directStreamByteReadControl = Measure(() =>
{
    var stream = new BitPdfStream(hotBytes);
    long sum = 0;
    int value;
    while ((value = stream.GetByte()) >= 0) sum += value;
    return sum;
});
byte[] tokens = Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("123 45 /Name (text) [ 1 2 3 ] << /Key true >>\n", 20000)));
var lexer = Measure(() =>
{
    var reader = new BitPdfLexer(new BitPdfStream(tokens));
    int count = 0;
    while (!ReferenceEquals(reader.GetObj(), BitPdfPrimitives.EOF)) count++;
    return count;
});
Console.WriteLine(JsonSerializer.Serialize(new
{
    runtime = Environment.Version.ToString(),
    architecture = RuntimeInformation.ProcessArchitecture.ToString(),
    tieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"),
    affinityMask = affinity,
    iterations,
    warmups,
    results,
    directStreamByteReadControl,
    lexer,
    peakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64,
}, new JsonSerializerOptions { WriteIndented = true }));
return 0;

object Measure(Func<object> operation)
{
    for (int i = 0; i < warmups; i++) GC.KeepAlive(operation());
    var elapsed = new double[iterations];
    var allocated = new long[iterations];
    for (int i = 0; i < iterations; i++)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        long start = Stopwatch.GetTimestamp();
        object result = operation();
        elapsed[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        allocated[i] = GC.GetAllocatedBytesForCurrentThread() - before;
        GC.KeepAlive(result);
    }
    return new { milliseconds = elapsed, allocatedBytes = allocated };
}

string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
