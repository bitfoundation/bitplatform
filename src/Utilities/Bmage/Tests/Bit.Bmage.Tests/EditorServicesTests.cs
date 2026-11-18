using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bmage.Tests;

/// <summary>
/// Where the editor components take their services from. An app that lazy loads Bit.Bmage on WebAssembly cannot call
/// AddBitBmageServices, so the editor has to build its own; an app that did register keeps exactly what it registered.
/// </summary>
[TestClass]
public sealed class EditorServicesTests
{
    [TestMethod]
    public async Task AnAppThatRegisteredBmageKeepsItsOwnServices()
    {
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddBitBmageServices().BuildServiceProvider();

        Assert.AreSame(app, BitBmageBlazor.GetEditorServices(app));
    }

    [TestMethod]
    public void WithoutTheRegistrationTheEditorBuildsItsServicesOncePerBrowserConnection()
    {
        var jsRuntime = new NoJSRuntime();
        using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var otherConnection = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).BuildServiceProvider();

        var services = BitBmageBlazor.GetEditorServices(first);

        Assert.IsNotNull(services.GetService<BmageImageLoader>());
        Assert.AreSame(services.GetRequiredService<BmageBrowserImageInterop>(), services.GetRequiredService<BmageBrowserImageInterop>());
        Assert.AreSame(services, BitBmageBlazor.GetEditorServices(second));
        Assert.AreNotSame(services, BitBmageBlazor.GetEditorServices(otherConnection));
    }

    [TestMethod]
    public async Task APartialRegistrationKeepsItsOptionsButNeverMixesItsServicesWithTheDefaults()
    {
        await using var app = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new NoJSRuntime())
            .AddBitBmageHeadlessServices(o => o.MaxSourceBytes = 1234)
            .BuildServiceProvider();

        var services = BitBmageBlazor.GetEditorServices(app);

        Assert.AreNotSame(app, services);
        Assert.AreSame(app.GetRequiredService<BmageOptions>(), services.GetRequiredService<BmageOptions>());
        Assert.AreEqual(1234, services.GetRequiredService<BmageImageLoader>().MaxSourceBytes);
        Assert.IsInstanceOfType<BmageBrowserCapabilityProvider>(services.GetRequiredService<IBmageImageCapabilityProvider>());
    }

    [TestMethod]
    public async Task TheBrowserTextRasterizerIsHeldToTheConfiguredLimits()
    {
        var limits = BmageImageLimits.Default with { MaxPixels = 2_000_000 };
        await using var app = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).AddBitBmageServices(o => o.Limits = limits).BuildServiceProvider();

        Assert.AreSame(limits, app.GetRequiredService<BmageBrowserTextRasterizer>().Limits);
    }

    [TestMethod]
    public async Task OverAServerCircuitTheServersCodecsHandleEveryFormatTheyCan()
    {
        var all = new HashSet<BmageImageFormat> { BmageImageFormat.Png, BmageImageFormat.Jpeg, BmageImageFormat.Bmp, BmageImageFormat.Gif, BmageImageFormat.WebP, BmageImageFormat.Avif };
        foreach (var environment in new[] { BmageImageEnvironment.BrowserServer, BmageImageEnvironment.BrowserWebAssembly })
        {
            var capabilities = new BmageImageCapabilities { Environment = environment, DecodableFormats = all, EncodableFormats = all };
            await using var interop = new BmageBrowserImageInterop(new NoJSRuntime());
            var processor = await new BmageImageProcessorFactory(interop, new FixedCapabilities(capabilities)).GetAsync();
            var codecs = processor.Codecs;

            if (environment == BmageImageEnvironment.BrowserServer)
            {
                // Pixels a browser codec touches would cross the circuit; the managed decoder goes first and the browser
                // only gets the files it rejects.
                Assert.IsInstanceOfType<BmageFallbackImageDecoder>(codecs.GetDecoder(BmageImageFormat.Jpeg));
                Assert.IsInstanceOfType<BmagePngEncoder>(codecs.GetEncoder(BmageImageFormat.Png));
                Assert.IsInstanceOfType<BmageJpegEncoder>(codecs.GetEncoder(BmageImageFormat.Jpeg));
                Assert.IsInstanceOfType<BmageFallbackImageDecoder>(codecs.GetDecoder(BmageImageFormat.WebP));
                Assert.IsInstanceOfType<BmageWebPEncoder>(codecs.GetEncoder(BmageImageFormat.WebP));
                Assert.IsInstanceOfType<BmageFallbackImageDecoder>(codecs.GetDecoder(BmageImageFormat.Avif));
                // Only a browser writes this one.
                Assert.IsInstanceOfType<BmageBrowserImageEncoder>(codecs.GetEncoder(BmageImageFormat.Avif));
            }
            else
            {
                Assert.IsInstanceOfType<BmageBrowserImageDecoder>(codecs.GetDecoder(BmageImageFormat.Jpeg));
                Assert.IsInstanceOfType<BmageBrowserImageEncoder>(codecs.GetEncoder(BmageImageFormat.Jpeg));
                Assert.IsInstanceOfType<BmageBrowserImageDecoder>(codecs.GetDecoder(BmageImageFormat.WebP));
                Assert.IsInstanceOfType<BmageBrowserImageEncoder>(codecs.GetEncoder(BmageImageFormat.WebP));
            }
        }
    }

    [TestMethod]
    public async Task InABrowserTheManagedCodecsCoverOnlyWhatTheBrowserLacks()
    {
        // A browser that decodes WebP but cannot write it, as Safari's canvas cannot: its own decoder keeps WebP, and the
        // managed encoder writes it when asked. In WebAssembly that encoder would hold the UI thread, so the capabilities,
        // built as BmageBrowserCapabilityProvider builds them there, do not offer it, and a fallback choice takes JPEG.
        var probed = new HashSet<BmageImageFormat> { BmageImageFormat.Png, BmageImageFormat.Jpeg, BmageImageFormat.WebP };
        var capabilities = new BmageImageCapabilities
        {
            Environment = BmageImageEnvironment.BrowserWebAssembly,
            DecodableFormats = probed.Union(BmageImageCapabilities.ManagedDecodableFormats).ToHashSet(),
            EncodableFormats = new HashSet<BmageImageFormat> { BmageImageFormat.Png, BmageImageFormat.Jpeg, BmageImageFormat.Bmp },
            NativeDecodableFormats = probed,
            NativeEncodableFormats = new HashSet<BmageImageFormat> { BmageImageFormat.Png, BmageImageFormat.Jpeg },
        };
        await using var interop = new BmageBrowserImageInterop(new NoJSRuntime());
        var codecs = (await new BmageImageProcessorFactory(interop, new FixedCapabilities(capabilities)).GetAsync()).Codecs;

        Assert.IsInstanceOfType<BmageBrowserImageDecoder>(codecs.GetDecoder(BmageImageFormat.WebP));
        Assert.IsInstanceOfType<BmageWebPEncoder>(codecs.GetEncoder(BmageImageFormat.WebP));
        Assert.IsInstanceOfType<BmageGifDecoder>(codecs.GetDecoder(BmageImageFormat.Gif), "the browser never claimed GIF, so it stays managed");
        Assert.IsInstanceOfType<BmageAvifDecoder>(codecs.GetDecoder(BmageImageFormat.Avif), "a browser without AVIF still reads it, through the managed decoder");
        Assert.IsTrue(capabilities.CanDecode(BmageImageFormat.Avif));
        Assert.AreEqual(BmageImageFormat.Jpeg, capabilities.ChooseEncodableFormat([BmageImageFormat.WebP, BmageImageFormat.Jpeg]));

        // A provider that wraps another carries the browser's own formats over, now that they are public.
        var wrapped = new BmageImageCapabilities
        {
            Environment = capabilities.Environment,
            DecodableFormats = capabilities.DecodableFormats,
            EncodableFormats = capabilities.EncodableFormats,
            NativeDecodableFormats = capabilities.NativeDecodableFormats,
            NativeEncodableFormats = capabilities.NativeEncodableFormats,
        };
        var wrappedCodecs = (await new BmageImageProcessorFactory(interop, new FixedCapabilities(wrapped)).GetAsync()).Codecs;
        Assert.IsInstanceOfType<BmageAvifDecoder>(wrappedCodecs.GetDecoder(BmageImageFormat.Avif));
        Assert.IsInstanceOfType<BmageWebPEncoder>(wrappedCodecs.GetEncoder(BmageImageFormat.WebP));
    }

    [TestMethod]
    public async Task TheBrowsersOwnFormatsAreWhatItsProbeReported()
    {
        // The probe's answer as the module returns it: a type no format has is passed over, and PNG, JPEG and BMP, which
        // every browser that runs Blazor decodes, are taken as read.
        const string report = """
            {
              "decodableMimeTypes": ["image/webp", "image/avif", "image/gif", "image/x-unknown"],
              "encodableMimeTypes": ["image/png", "image/webp"],
              "maxCanvasDimension": 16384
            }
            """;
        var jsRuntime = new ProbedJSRuntime(report);
        await using var interop = new BmageBrowserImageInterop(jsRuntime);
        var capabilities = await new BmageBrowserCapabilityProvider(interop, jsRuntime).GetAsync();

        CollectionAssert.AreEquivalent(
            new[] { BmageImageFormat.Png, BmageImageFormat.Jpeg, BmageImageFormat.Bmp, BmageImageFormat.WebP, BmageImageFormat.Avif, BmageImageFormat.Gif },
            capabilities.NativeDecodableFormats!.ToArray());
        CollectionAssert.AreEquivalent(new[] { BmageImageFormat.Png, BmageImageFormat.WebP }, capabilities.NativeEncodableFormats!.ToArray());
        // Off WebAssembly the managed codecs join the full sets, never the browser's own.
        Assert.AreEqual(BmageImageEnvironment.BrowserServer, capabilities.Environment);
        Assert.IsTrue(capabilities.CanEncode(BmageImageFormat.Jpeg));
        Assert.IsTrue(capabilities.CanEncode(BmageImageFormat.Bmp));
    }

    [TestMethod]
    public async Task TheBrowserDecodesWhatTheManagedDecoderRefusesOrIsToldToLeave()
    {
        var managed = new FakeDecoder("managed");
        var browser = new FakeDecoder("browser");
        var decoder = new BmageFallbackImageDecoder(BmageImageFormat.Jpeg, managed, browser, data => data.Length == 2);

        async Task<string> DecodeWith(byte[] data, Exception? managedFails = null)
        {
            managed.Fails = managedFails;
            using var image = await decoder.DecodeAsync(data, new BmageDecodeOptions(), BmagePooledPixelAllocator.Shared);
            return image.Metadata!.FileName!;
        }

        Assert.AreEqual("managed", await DecodeWith([1]));
        // Over the limits the managed decoder refuses where the browser scales down; a file it cannot read the browser may.
        Assert.AreEqual("browser", await DecodeWith([1], new BmageImageLimitExceededException("too large")));
        Assert.AreEqual("browser", await DecodeWith([1], new BmageImageDecodeException("unreadable")));
        // Sent straight to the browser, as a photo with a wide gamut colour profile is.
        Assert.AreEqual("browser", await DecodeWith([1, 2]));
        Assert.AreEqual(3, managed.Calls, "a file sent straight to the browser must not reach the managed decoder");
    }

    private sealed class FakeDecoder(string name) : IBmageImageDecoder
    {
        public Exception? Fails { get; set; }
        public int Calls { get; private set; }
        public IReadOnlyCollection<BmageImageFormat> Formats { get; } = [BmageImageFormat.Jpeg];
        public BmageImageInfo? Identify(ReadOnlySpan<byte> data) => null;
        public ValueTask<BmageImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, BmageDecodeOptions options, IBmagePixelAllocator allocator, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Fails is { } failure) throw failure;
            var image = BmageImageBuffer.Create(1, 1);
            image.Metadata = new BmageImageMetadata(fileName: name);
            return ValueTask.FromResult(image);
        }
    }

    private sealed class FixedCapabilities(BmageImageCapabilities capabilities) : IBmageImageCapabilityProvider
    {
        public ValueTask<BmageImageCapabilities> GetAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(capabilities);
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
    }

    /// <summary>
    /// A browser whose module answers the capability probe with a fixed report, read as the real one is, and does nothing
    /// for the calls that return nothing, such as releasing its resources.
    /// </summary>
    private sealed class ProbedJSRuntime(string report) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => identifier == "import" ? ValueTask.FromResult((TValue)(object)new Module(report)) : throw new NotSupportedException();

        private sealed class Module(string report) : IJSObjectReference
        {
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);
            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
                => identifier == "getCapabilities" ? ValueTask.FromResult(JsonSerializer.Deserialize<TValue>(report)!)
                    : typeof(TValue) == typeof(Microsoft.JSInterop.Infrastructure.IJSVoidResult) ? ValueTask.FromResult(default(TValue)!)
                    : throw new NotSupportedException();
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
