using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bink.Tests.Components;

/// <summary>
/// On Blazor Server an interop call completes on a thread-pool thread. These tests complete the canvas's interop
/// calls that way, which is what took a Blazor Server circuit down when the canvas raised Ready off the dispatcher.
/// </summary>
[TestClass]
public sealed class BinkCanvasDispatcherTests : BunitContext
{
    private const string ModulePath = "./_content/Bit.Bink/bit-bink.js";

    public BinkCanvasDispatcherTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    /// <summary>A parent whose Ready handler re-renders it, as the README's examples and the demo pages do.</summary>
    private sealed class ReadyHost : ComponentBase
    {
        public int Renders { get; private set; }

        public bool IsReady { get; private set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            Renders++;
            builder.OpenComponent<BinkCanvas>(0);
            builder.AddAttribute(1, nameof(BinkCanvas.Ready), EventCallback.Factory.Create<BinkInputCapabilities>(this, _ => IsReady = true));
            builder.CloseComponent();
        }
    }

    [TestMethod]
    public async Task Ready_is_raised_on_the_dispatcher_when_the_browser_answers_from_another_thread()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        JSRuntimeInvocationHandler<BinkAttachResult?> attach = module.Setup<BinkAttachResult?>("attach", _ => true);

        IRenderedComponent<ReadyHost> host = Render<ReadyHost>();
        int before = host.Instance.Renders;

        await Task.Run(() => attach.SetResult(new BinkAttachResult { Width = 400, Height = 300, DevicePixelRatio = 1 }));

        // Raised off the dispatcher, the host's re-render threw "The current thread is not associated with the
        // Dispatcher", which on Blazor Server ends the circuit.
        host.WaitForAssertion(() =>
        {
            Assert.IsTrue(host.Instance.IsReady);
            Assert.IsGreaterThan(before, host.Instance.Renders);
        }, TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task An_image_added_after_a_browser_round_trip_is_added_on_the_dispatcher()
    {
        BunitJSModuleInterop module = JSInterop.SetupModule(ModulePath);
        module.Mode = JSRuntimeMode.Loose;
        module.Setup<BinkAttachResult?>("attach", _ => true).SetResult(new BinkAttachResult { Width = 400, Height = 300, DevicePixelRatio = 1 });
        JSRuntimeInvocationHandler<BinkImageMetrics> load = module.Setup<BinkImageMetrics>("loadImage", _ => true);
        IRenderedComponent<BinkCanvas> canvas = Render<BinkCanvas>();
        bool? onDispatcher = null;
        Dispatcher dispatcher = Renderer.Dispatcher;
        await canvas.InvokeAsync(() => canvas.Instance.CurrentDocument.Changed += (_, _) => onDispatcher = dispatcher.CheckAccess());

        BinkImageSource source = BinkImageSource.FromBytes(TestData.CreatePng(4, 4, (_, _) => BinkColor.Black), "image/png", 0, 0);
        Task<BinkImageElement?> adding = canvas.InvokeAsync(() => canvas.Instance.AddImageAsync(source));
        await Task.Run(() => load.SetResult(new BinkImageMetrics { Width = 4, Height = 4 }));
        Assert.IsNotNull(await adding);

        // The document is not thread-safe: changing it from the thread the browser's answer arrived on raced the
        // dispatcher, which is processing pointer input into the same document. The dispatcher guarantees mutual
        // exclusion rather than one thread, so access is what is checked, not a thread id.
        Assert.IsTrue(onDispatcher);
    }
}
