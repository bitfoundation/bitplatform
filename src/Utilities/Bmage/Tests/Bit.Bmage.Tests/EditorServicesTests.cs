using Bit.Bmage.Input;
using Bit.Bmage.Interop;
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

        Assert.AreSame(app, BitBmage.GetEditorServices(app));
    }

    [TestMethod]
    public void WithoutTheRegistrationTheEditorBuildsItsServicesOncePerBrowserConnection()
    {
        var jsRuntime = new NoJSRuntime();
        using var first = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var second = new ServiceCollection().AddSingleton<IJSRuntime>(jsRuntime).BuildServiceProvider();
        using var otherConnection = new ServiceCollection().AddSingleton<IJSRuntime>(new NoJSRuntime()).BuildServiceProvider();

        var services = BitBmage.GetEditorServices(first);

        Assert.IsNotNull(services.GetService<ImageLoader>());
        Assert.AreSame(services.GetRequiredService<BrowserImageInterop>(), services.GetRequiredService<BrowserImageInterop>());
        Assert.AreSame(services, BitBmage.GetEditorServices(second));
        Assert.AreNotSame(services, BitBmage.GetEditorServices(otherConnection));
    }

    [TestMethod]
    public async Task APartialRegistrationKeepsItsOptionsButNeverMixesItsServicesWithTheDefaults()
    {
        await using var app = new ServiceCollection()
            .AddSingleton<IJSRuntime>(new NoJSRuntime())
            .AddBitBmageHeadlessServices(o => o.MaxSourceBytes = 1234)
            .BuildServiceProvider();

        var services = BitBmage.GetEditorServices(app);

        Assert.AreNotSame(app, services);
        Assert.AreSame(app.GetRequiredService<BitBmageOptions>(), services.GetRequiredService<BitBmageOptions>());
        Assert.AreEqual(1234, services.GetRequiredService<ImageLoader>().MaxSourceBytes);
        Assert.IsInstanceOfType<Capabilities.BrowserCapabilityProvider>(services.GetRequiredService<Capabilities.IImageCapabilityProvider>());
    }

    [TestMethod]
    public async Task OverAServerCircuitTheServersCodecsHandleEveryFormatTheyCan()
    {
        var all = new HashSet<ImageFormat> { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.Bmp, ImageFormat.Gif, ImageFormat.WebP, ImageFormat.Avif };
        foreach (var environment in new[] { Capabilities.ImageEnvironment.BrowserServer, Capabilities.ImageEnvironment.BrowserWebAssembly })
        {
            var capabilities = new Capabilities.ImageCapabilities { Environment = environment, DecodableFormats = all, EncodableFormats = all };
            await using var interop = new BrowserImageInterop(new NoJSRuntime());
            var processor = await new ImageProcessorFactory(interop, new FixedCapabilities(capabilities)).GetAsync();
            var codecs = processor.Codecs;

            if (environment == Capabilities.ImageEnvironment.BrowserServer)
            {
                // Pixels a browser codec touches would cross the circuit; the managed decoder goes first and the browser
                // only gets the files it rejects.
                Assert.IsInstanceOfType<Codecs.FallbackImageDecoder>(codecs.GetDecoder(ImageFormat.Jpeg));
                Assert.IsInstanceOfType<Codecs.Png.PngEncoder>(codecs.GetEncoder(ImageFormat.Png));
                Assert.IsInstanceOfType<Codecs.Jpeg.JpegEncoder>(codecs.GetEncoder(ImageFormat.Jpeg));
                Assert.IsInstanceOfType<Codecs.FallbackImageDecoder>(codecs.GetDecoder(ImageFormat.WebP));
                Assert.IsInstanceOfType<Codecs.WebP.WebPEncoder>(codecs.GetEncoder(ImageFormat.WebP));
                Assert.IsInstanceOfType<Codecs.FallbackImageDecoder>(codecs.GetDecoder(ImageFormat.Avif));
                // Only a browser writes this one.
                Assert.IsInstanceOfType<Codecs.Browser.BrowserImageEncoder>(codecs.GetEncoder(ImageFormat.Avif));
            }
            else
            {
                Assert.IsInstanceOfType<Codecs.Browser.BrowserImageDecoder>(codecs.GetDecoder(ImageFormat.Jpeg));
                Assert.IsInstanceOfType<Codecs.Browser.BrowserImageEncoder>(codecs.GetEncoder(ImageFormat.Jpeg));
                Assert.IsInstanceOfType<Codecs.Browser.BrowserImageDecoder>(codecs.GetDecoder(ImageFormat.WebP));
                Assert.IsInstanceOfType<Codecs.Browser.BrowserImageEncoder>(codecs.GetEncoder(ImageFormat.WebP));
            }
        }
    }

    [TestMethod]
    public async Task InABrowserTheManagedCodecsCoverOnlyWhatTheBrowserLacks()
    {
        // A browser that decodes WebP but cannot write it, as Safari's canvas cannot: its own decoder keeps WebP, the
        // managed encoder writes it, and the capabilities say so.
        var probed = new HashSet<ImageFormat> { ImageFormat.Png, ImageFormat.Jpeg, ImageFormat.WebP };
        var capabilities = new Capabilities.ImageCapabilities
        {
            Environment = Capabilities.ImageEnvironment.BrowserWebAssembly,
            DecodableFormats = probed.Union(Capabilities.ImageCapabilities.ManagedDecodableFormats).ToHashSet(),
            EncodableFormats = Capabilities.ImageCapabilities.ManagedEncodableFormats.Union([ImageFormat.Png, ImageFormat.Jpeg]).ToHashSet(),
            NativeDecodableFormats = probed,
            NativeEncodableFormats = new HashSet<ImageFormat> { ImageFormat.Png, ImageFormat.Jpeg },
        };
        await using var interop = new BrowserImageInterop(new NoJSRuntime());
        var codecs = (await new ImageProcessorFactory(interop, new FixedCapabilities(capabilities)).GetAsync()).Codecs;

        Assert.IsInstanceOfType<Codecs.Browser.BrowserImageDecoder>(codecs.GetDecoder(ImageFormat.WebP));
        Assert.IsInstanceOfType<Codecs.WebP.WebPEncoder>(codecs.GetEncoder(ImageFormat.WebP));
        Assert.IsInstanceOfType<Codecs.Gif.GifDecoder>(codecs.GetDecoder(ImageFormat.Gif), "the browser never claimed GIF, so it stays managed");
        Assert.IsInstanceOfType<Codecs.Avif.AvifDecoder>(codecs.GetDecoder(ImageFormat.Avif), "a browser without AVIF still reads it, through the managed decoder");
        Assert.IsTrue(capabilities.CanDecode(ImageFormat.Avif));
        Assert.AreEqual(ImageFormat.WebP, capabilities.ChooseEncodableFormat([ImageFormat.WebP, ImageFormat.Jpeg]));
    }

    [TestMethod]
    public async Task TheBrowserDecodesWhatTheManagedDecoderRefusesOrIsToldToLeave()
    {
        var managed = new FakeDecoder("managed");
        var browser = new FakeDecoder("browser");
        var decoder = new Codecs.FallbackImageDecoder(ImageFormat.Jpeg, managed, browser, data => data.Length == 2);

        async Task<string> DecodeWith(byte[] data, Exception? managedFails = null)
        {
            managed.Fails = managedFails;
            using var image = await decoder.DecodeAsync(data, new Codecs.DecodeOptions(), Memory.PooledPixelAllocator.Shared);
            return image.Metadata!.FileName!;
        }

        Assert.AreEqual("managed", await DecodeWith([1]));
        // Over the limits the managed decoder refuses where the browser scales down; a file it cannot read the browser may.
        Assert.AreEqual("browser", await DecodeWith([1], new ImageLimitExceededException("too large")));
        Assert.AreEqual("browser", await DecodeWith([1], new ImageDecodeException("unreadable")));
        // Sent straight to the browser, as a photo with a wide gamut colour profile is.
        Assert.AreEqual("browser", await DecodeWith([1, 2]));
        Assert.AreEqual(3, managed.Calls, "a file sent straight to the browser must not reach the managed decoder");
    }

    private sealed class FakeDecoder(string name) : Codecs.IImageDecoder
    {
        public Exception? Fails { get; set; }
        public int Calls { get; private set; }
        public IReadOnlyCollection<ImageFormat> Formats { get; } = [ImageFormat.Jpeg];
        public Codecs.ImageInfo? Identify(ReadOnlySpan<byte> data) => null;
        public ValueTask<ImageBuffer> DecodeAsync(ReadOnlyMemory<byte> data, Codecs.DecodeOptions options, Memory.IPixelAllocator allocator, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Fails is { } failure) throw failure;
            var image = ImageBuffer.Create(1, 1);
            image.Metadata = new Metadata.ImageMetadata(fileName: name);
            return ValueTask.FromResult(image);
        }
    }

    private sealed class FixedCapabilities(Capabilities.ImageCapabilities capabilities) : Capabilities.IImageCapabilityProvider
    {
        public ValueTask<Capabilities.ImageCapabilities> GetAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(capabilities);
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
    }
}
