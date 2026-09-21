using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Extensions.JsInterop;

/// <summary>
/// Renders the components that were moved onto <c>FastInvoke</c> against a runtime that really is
/// <see cref="IJSInProcessRuntime"/>, the way Blazor WebAssembly's is.
///
/// <para>
/// Every other test in this project renders against bUnit's runtime, which is not in-process, so all of them
/// exercise the asynchronous fallback - the path Blazor Server and Hybrid take. The synchronous path is the
/// one WebAssembly, the library's main host, actually runs, and it differs in ways a component can feel: the
/// call completes before the next statement, its result is there without a yield, and an error surfaces at
/// the call rather than on a continuation. These tests pin that a component still sets itself up, reacts to
/// parameter changes and tears down correctly when its interop behaves that way.
/// </para>
/// </summary>
[TestClass]
public class FastInvokeInProcessComponentTests : BunitTestContext
{
    private InProcessJsRuntime _js = default!;

    [TestInitialize]
    public void Init()
    {
        _js = new InProcessJsRuntime();

        // Registered last, so it is what the components resolve instead of bUnit's own (not in-process) one.
        Services.AddSingleton<IJSRuntime>(_js);
        Services.AddScoped(_ => new BitPageVisibility(_js));
    }


    [TestMethod]
    public void TheTestRuntimeMustBeTheOneComponentsResolve()
    {
        // Everything below is worthless if bUnit's runtime is still the one being injected, and that would
        // show up as components that simply record nothing rather than as a failure.
        var jsRuntime = Services.GetRequiredService<IJSRuntime>();

        Assert.IsInstanceOfType<InProcessJsRuntime>(jsRuntime);
        Assert.IsInstanceOfType<IJSInProcessRuntime>(jsRuntime);
        Assert.IsFalse(jsRuntime.IsRuntimeInvalid(), "a WebAssembly runtime must be seen as valid by the interop guard");
    }


    [TestMethod]
    public void BitSwipeTrapShouldSetUpAndTearDownThroughTheSynchronousPath()
    {
        RenderComponent<BitSwipeTrap>(parameters => parameters.AddChildContent("<p>swipe</p>"));

        var setup = _js.SyncInvocations.Single(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup");
        Assert.IsTrue(setup.Args!.OfType<DotNetObjectReference<BitSwipeTrap>>().Any(),
            "the setup must be handed the component's own .NET reference");
        Assert.AreEqual(0, _js.AsyncInvocations.Count, "nothing may fall back to the asynchronous path on WebAssembly");

        DisposeComponents();

        Assert.AreEqual(1, _js.CountOf("BitBlazorUI.SwipeTrap.dispose"));
        Assert.AreEqual(0, _js.AsyncInvocations.Count);
    }

    [TestMethod]
    public void BitSwipeTrapShouldReSetUpWithAFreshReferenceWhenItsParametersChange()
    {
        // Each setup gets a fresh reference and releases the previous one. A reference released while the JS
        // side still held it would show up as a callback into a disposed object; one never released is a leak
        // for the life of the page. Both hinge on the setup/teardown pairing below.
        var component = RenderComponent<BitSwipeTrap>(parameters => parameters.AddChildContent("<p>swipe</p>"));

        component.Render(parameters => parameters.Add(p => p.Threshold, 42));

        Assert.AreEqual(2, _js.CountOf("BitBlazorUI.SwipeTrap.setup"));
        Assert.AreEqual(1, _js.CountOf("BitBlazorUI.SwipeTrap.dispose"), "the previous setup must be torn down before the new one");

        var references = _js.AllInvocations
            .Where(i => i.Identifier == "BitBlazorUI.SwipeTrap.setup")
            .Select(i => i.Args!.OfType<DotNetObjectReference<BitSwipeTrap>>().Single())
            .ToList();

        Assert.AreNotSame(references[0], references[1], "each setup must be handed its own .NET reference");
    }


    [TestMethod]
    public void BitPullToRefreshShouldSetUpThroughTheSynchronousPath()
    {
        RenderComponent<BitPullToRefresh>(parameters => parameters.AddChildContent("<p>pull</p>"));

        Assert.AreEqual(1, _js.CountOf("BitBlazorUI.PullToRefresh.setup"));
        Assert.AreEqual(0, _js.AsyncInvocations.Count,
            "PullToRefresh is fully on the fast path, so nothing of it may fall back to the asynchronous one");

        DisposeComponents();

        Assert.AreEqual(1, _js.CountOf("BitBlazorUI.PullToRefresh.dispose"));
    }

    [TestMethod]
    public void BitPullToRefreshShouldHandItsOwnDotNetReferenceToTheSetup()
    {
        RenderComponent<BitPullToRefresh>(parameters => parameters.AddChildContent("<p>pull</p>"));

        var setup = _js.SyncInvocations.Single(i => i.Identifier == "BitBlazorUI.PullToRefresh.setup");

        Assert.IsTrue(setup.Args!.OfType<DotNetObjectReference<BitPullToRefresh>>().Any(),
            "the setup must be handed the component's own .NET reference, which it now owns and disposes");
    }


    [TestMethod]
    public void BitSwiperShouldSetUpAndTearDownThroughTheSynchronousPath()
    {
        RenderComponent<BitSwiper>(parameters =>
        {
            parameters.AddChildContent<BitSwiperItem>(item => item.AddChildContent("<span>one</span>"));
        });

        var setup = _js.SyncInvocations.Single(i => i.Identifier == "BitBlazorUI.Swiper.setup");
        Assert.IsTrue(setup.Args!.OfType<DotNetObjectReference<BitSwiper>>().Any());

        DisposeComponents();

        Assert.AreEqual(1, _js.CountOf("BitBlazorUI.Swiper.dispose"));
    }


    [TestMethod]
    public void BitBreadcrumbShouldKeepItsDotNetReferenceWhenItStopsObserving()
    {
        // The breadcrumb hands one reference to the resize observer, to its overflow callout and to its
        // keyboard handling, and it stops observing the moment auto-collapsing is turned off - while the
        // component stays on the page. Releasing the reference from the JS teardown (which is what
        // Observers.unregisterResize used to do) took the callout and the keyboard with it, silently.
        var component = RenderComponent<BitBreadcrumb<BitBreadcrumbItem>>(parameters =>
        {
            parameters.Add(p => p.AutoCollapse, true);
            parameters.Add(p => p.Items, new List<BitBreadcrumbItem> { new() { Text = "one" }, new() { Text = "two" } });
        });

        var register = _js.AllInvocations.SingleOrDefault(i => i.Identifier == "BitBlazorUI.Observers.registerResize");
        Assert.IsNotNull(register, "an auto-collapsing breadcrumb follows its own width");

        var reference = register.Args!.OfType<DotNetObjectReference<BitBreadcrumb<BitBreadcrumbItem>>>().Single();

        component.Render(parameters => parameters.Add(p => p.AutoCollapse, false));

        Assert.AreEqual(1, _js.CountOf("BitBlazorUI.Observers.unregisterResize"), "it must stop observing");

        // The reference is still the component's, and the component is still on the page.
        Assert.IsNotNull(reference.Value);

        var unregister = _js.AllInvocations.Single(i => i.Identifier == "BitBlazorUI.Observers.unregisterResize");
        CollectionAssert.AreEqual(new object?[] { component.Instance.UniqueId.ToString() }, unregister.Args,
            "the id alone identifies the observer; handing the element and the reference over again is what " +
            "let the JS side unobserve a stale element and release a reference that was not its to release");
    }


    [TestMethod]
    public void BitCarouselShouldLayOutWhenTheMeasurementAnswersWithNothing()
    {
        // Utils.getBoundingClientRect answers with null now - for an element that is not there, and for a
        // measurement that failed - where it used to answer with an empty object that deserialized into a rect
        // of all zeros. The carousel has to read that as "no measurement" and lay out on its defaults rather
        // than on a bogus width of zero, and certainly not throw.
        var component = RenderComponent<BitCarousel>(parameters =>
        {
            parameters.AddChildContent<BitCarouselItem>(item => item.AddChildContent("<span>one</span>"));
            parameters.AddChildContent<BitCarouselItem>(item => item.AddChildContent("<span>two</span>"));
        });

        Assert.AreEqual(2, component.FindAll(".bit-crsi").Count);
        Assert.IsTrue(_js.CountOf("BitBlazorUI.Utils.getBoundingClientRect") > 0,
            "the carousel measures its container before laying out");
    }


    [TestMethod]
    public void BitColorPickerShouldCarryTheSetupResultIntoItsTeardown()
    {
        // The setup's return value is read straight off the synchronous call - no await in between - and is
        // what the teardown has to hand back. A fast path that dropped the result would leave the JS side's
        // listeners attached for the life of the page.
        _js.Handlers["BitBlazorUI.ColorPicker.setup"] = _ => "abort-id-1";

        RenderComponent<BitColorPicker>();

        Assert.AreEqual(1, _js.CountOf("BitBlazorUI.ColorPicker.setup"));

        DisposeComponents();

        var dispose = _js.SyncInvocations.SingleOrDefault(i => i.Identifier == "BitBlazorUI.ColorPicker.dispose");
        Assert.IsNotNull(dispose, "the teardown must reach JavaScript");
        CollectionAssert.AreEqual(new object?[] { "abort-id-1" }, dispose.Args);
    }


    [TestMethod]
    public void BitFileUploadShouldSurviveADragDropSetupThatAnswersWithNothing()
    {
        // FastInvoke answers with default when the runtime can't service interop, and the JS side can answer
        // with null too, so the drop-zone reference is nullable now. A parameter change then has nothing to
        // update and must simply do nothing instead of throwing.
        _js.Handlers["BitBlazorUI.FileUpload.setupDragDrop"] = _ => null;

        var component = RenderComponent<BitFileUpload>();

        component.Render(parameters => parameters.Add(p => p.AllowDrop, false));

        Assert.AreEqual(1, _js.CountOf("BitBlazorUI.FileUpload.setupDragDrop"));
    }

    [TestMethod]
    public void BitFileUploadShouldUpdateTheDropZoneItWasGiven()
    {
        var dropZone = new FakeJsObjectReference();
        _js.Handlers["BitBlazorUI.FileUpload.setupDragDrop"] = _ => dropZone;

        var component = RenderComponent<BitFileUpload>();

        component.Render(parameters => parameters.Add(p => p.AllowDrop, false));

        Assert.IsTrue(dropZone.Invocations.Contains("update"),
            "a parameter change must be pushed to the drop zone the setup answered with");
    }


    [TestMethod]
    public void AComponentShouldRenderThroughAJsonFailureOnTheSynchronousPath()
    {
        // A JSON problem on the in-process path is reported and swallowed, unlike a JSException. A render must
        // therefore complete rather than tear the component down.
        _js.ExceptionFactory = identifier => identifier == "BitBlazorUI.PullToRefresh.setup" ? new JsonException("bad json") : null;

        var component = RenderComponent<BitPullToRefresh>(parameters => parameters.AddChildContent("<p>pull</p>"));

        Assert.IsNotNull(component.Find(".bit-ptr"));
    }

    [TestMethod]
    public void AComponentShouldSurfaceAJsExceptionFromTheSynchronousPath()
    {
        // The other half of the same contract: a missing function, or one that throws, must not be swallowed -
        // it surfaces exactly as it would on the asynchronous path, so a broken interop wiring is loud.
        _js.ExceptionFactory = identifier => identifier == "BitBlazorUI.PullToRefresh.setup" ? new JSException("boom") : null;

        Assert.ThrowsExactly<JSException>(
            () => RenderComponent<BitPullToRefresh>(parameters => parameters.AddChildContent("<p>pull</p>")));
    }


    private void DisposeComponents() => Context.DisposeComponentsAsync().GetAwaiter().GetResult();


    private sealed record Invocation(string Identifier, object?[]? Args);

    /// <summary>
    /// An <see cref="IJSInProcessRuntime"/> that answers synchronously, the way Blazor WebAssembly's runtime
    /// does. Its type name is deliberately not one <c>IsRuntimeInvalid</c> knows, so it is treated as valid.
    /// </summary>
    private sealed class InProcessJsRuntime : IJSInProcessRuntime
    {
        public List<Invocation> SyncInvocations { get; } = [];
        public List<Invocation> AsyncInvocations { get; } = [];

        /// <summary>What a given JS function answers with; anything unlisted answers with default.</summary>
        public Dictionary<string, Func<object?[]?, object?>> Handlers { get; } = [];

        /// <summary>What a given JS function throws, if anything.</summary>
        public Func<string, Exception?>? ExceptionFactory { get; set; }

        /// <summary>Every invocation, whichever path it took.</summary>
        public IEnumerable<Invocation> AllInvocations => SyncInvocations.Concat(AsyncInvocations);

        public int CountOf(string identifier) => AllInvocations.Count(i => i.Identifier == identifier);

        public TResult Invoke<TResult>(string identifier, params object?[]? args)
        {
            SyncInvocations.Add(new Invocation(identifier, args));

            var exception = ExceptionFactory?.Invoke(identifier);
            if (exception is not null) throw exception;

            if (typeof(TResult) == typeof(IJSVoidResult)) return default!;

            return Handlers.TryGetValue(identifier, out var handler) ? (TResult)handler(args)! : default!;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            AsyncInvocations.Add(new Invocation(identifier, args));

            if (typeof(TValue) == typeof(IJSVoidResult)) return new ValueTask<TValue>(default(TValue)!);

            return new ValueTask<TValue>(Handlers.TryGetValue(identifier, out var handler) ? (TValue)handler(args)! : default!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }

    /// <summary>A stand-in for the drop zone the FileUpload drag/drop setup answers with.</summary>
    private sealed class FakeJsObjectReference : IJSObjectReference
    {
        public List<string> Invocations { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(identifier);
            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
