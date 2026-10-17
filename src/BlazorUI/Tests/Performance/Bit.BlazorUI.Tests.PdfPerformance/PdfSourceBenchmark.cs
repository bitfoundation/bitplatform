using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bit.BlazorUI;

// Separate-process comparisons include opening/I/O and initial raw storage.
// The seekable process never reads a full fixture array, even for hashes.
internal static class PdfSourceBenchmark
{
    public static int Run(string[] args)
    {
        if (args.Length < 5 || args[1] is not ("buffered" or "seekable")
            || !int.TryParse(args[2], out int iterations) || iterations <= 0
            || !int.TryParse(args[3], out int warmups) || warmups < 0)
        {
            Console.Error.WriteLine("Usage: --source <buffered|seekable> <iterations> <warmups> <pdf-path> [pdf-path ...]");
            return 1;
        }
        string? affinity = Environment.GetEnvironmentVariable("PDF_BENCH_AFFINITY");
        if (affinity is not null && (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()))
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(long.Parse(affinity));
        bool seekable = args[1] == "seekable";
        var results = new List<object>();
        foreach (string path in args.Skip(4))
        {
            long length = new FileInfo(path).Length;
            string sha256;
            using (var file = File.OpenRead(path)) sha256 = Convert.ToHexString(SHA256.HashData(file));
            int pages, warnings;
            string htmlHash, canvasHash;
            using (var opened = Open(path, seekable))
            {
                Prepare(opened.Document);
                pages = opened.Document.PageCount;
                if (pages == 0) throw new InvalidOperationException("Fixture has no pages.");
                htmlHash = Hash(new BitPdfHtmlRenderer(opened.Document.Pages[0], opened.Document.XRef).Render());
                canvasHash = Hash(JsonSerializer.Serialize(new BitPdfCanvasRenderer(opened.Document.Pages[0], opened.Document.XRef).Render()));
                warnings = opened.Document.Warnings.Count;
            }
            var metrics = new Dictionary<string, object>();
            foreach (string operation in new[] { "openAndPrepare", "openAndFirstPageHtml", "openAndFirstPageCanvas" })
            {
                var milliseconds = new double[iterations];
                var allocated = new long[iterations];
                var rawStorage = new long[iterations];
                for (int i = -warmups; i < iterations; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    long before = GC.GetAllocatedBytesForCurrentThread();
                    long start = Stopwatch.GetTimestamp();
                    using var opened = Open(path, seekable);
                    Prepare(opened.Document);
                    object result = operation switch
                    {
                        "openAndFirstPageHtml" => new BitPdfHtmlRenderer(opened.Document.Pages[0], opened.Document.XRef).Render(),
                        "openAndFirstPageCanvas" => new BitPdfCanvasRenderer(opened.Document.Pages[0], opened.Document.XRef).Render(),
                        _ => opened.Document.PageCount,
                    };
                    if (i >= 0)
                    {
                        milliseconds[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                        allocated[i] = GC.GetAllocatedBytesForCurrentThread() - before;
                        rawStorage[i] = opened.RawStoragePeak;
                    }
                    GC.KeepAlive(result);
                }
                metrics[operation] = new { milliseconds, allocatedBytes = allocated, rawStoragePeakBytes = rawStorage };
            }
            results.Add(new { fixture = Path.GetFileName(path), bytes = length, sha256, pages, warnings, htmlSha256 = htmlHash, canvasSha256 = canvasHash, metrics });
        }
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            mode = args[1], runtime = Environment.Version.ToString(), architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            tieredCompilation = Environment.GetEnvironmentVariable("DOTNET_TieredCompilation"), affinityMask = affinity,
            iterations, warmups, results, peakWorkingSetBytes = Process.GetCurrentProcess().PeakWorkingSet64,
        }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static void Prepare(BitPdfDocument document)
    {
        _ = document.Pages;
        _ = document.Metadata;
        _ = document.PageLabels;
        _ = document.Outline;
        _ = document.Attachments;
        _ = document.Layers;
        _ = document.FormFields;
        _ = document.StructureTree;
    }

    private static Opened Open(string path, bool seekable)
    {
        if (!seekable)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return new(BitPdfDocument.Load(bytes), null, bytes.LongLength);
        }
        var file = File.OpenRead(path);
        object? source = null;
        try
        {
            source = SeekableAccess.Create(file);
            return new(SeekableAccess.Load(source), source, 0);
        }
        catch
        {
            if (source is IAsyncDisposable owned) owned.DisposeAsync().AsTask().GetAwaiter().GetResult();
            else file.Dispose();
            throw;
        }
    }

    private sealed class Opened(BitPdfDocument document, object? source, long bufferSize) : IDisposable
    {
        public BitPdfDocument Document { get; } = document;
        public long RawStoragePeak => source is null ? bufferSize : SeekableAccess.Peak(source);
        public void Dispose()
        {
            if (source is not IAsyncDisposable owned) return;
            SeekableAccess.Release(Document);
            owned.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }

    // Compile the internal calls once, outside samples, without a public load API.
    private static class SeekableAccess
    {
        public static readonly Func<Stream, object> Create;
        public static readonly Func<object, BitPdfDocument> Load;
        public static readonly Func<object, int> Peak;
        public static readonly Action<BitPdfDocument> Release;

        static SeekableAccess()
        {
            var sourceType = typeof(BitPdfDocument).Assembly.GetType("Bit.BlazorUI.BitPdfSeekableStreamByteSource", true)!;
            var stream = Expression.Parameter(typeof(Stream));
            Create = Expression.Lambda<Func<Stream, object>>(Expression.New(sourceType.GetConstructors().Single(),
                stream, Expression.Constant(CancellationToken.None), Expression.Constant(16 * 1024 * 1024), Expression.Constant(64 * 1024)), stream).Compile();
            var source = Expression.Parameter(typeof(object));
            var load = typeof(BitPdfDocument).GetMethods(BindingFlags.Static | BindingFlags.NonPublic).Single(m => m.Name == "Load");
            Load = Expression.Lambda<Func<object, BitPdfDocument>>(Expression.Call(load,
                Expression.Convert(source, sourceType), Expression.Constant(null, typeof(string))), source).Compile();
            Peak = Expression.Lambda<Func<object, int>>(Expression.Property(Expression.Convert(source, sourceType),
                sourceType.GetProperty("PeakBytes", BindingFlags.Instance | BindingFlags.NonPublic)!), source).Compile();
            var document = Expression.Parameter(typeof(BitPdfDocument));
            Release = Expression.Lambda<Action<BitPdfDocument>>(Expression.Call(document,
                typeof(BitPdfDocument).GetMethod("ReleaseByteWindows", BindingFlags.Instance | BindingFlags.NonPublic)!), document).Compile();
        }
    }
}
