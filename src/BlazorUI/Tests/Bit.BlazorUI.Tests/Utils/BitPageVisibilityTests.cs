using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Bit.BlazorUI.Tests.Extensions.JsInterop;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

/// <summary>
/// <see cref="BitPageVisibility"/> installs page-wide listeners from JavaScript and hands the browser a
/// <see cref="DotNetObjectReference{T}"/> to call back into. It is registered as a scoped service, so its
/// teardown is the DI scope's - which is why it now offers both <see cref="IDisposable"/> and
/// <see cref="IAsyncDisposable"/>: a container torn down synchronously throws for a scoped service that only
/// offers the asynchronous one.
/// </summary>
[TestClass]
public class BitPageVisibilityTests
{
    [TestMethod]
    public async Task InitShouldInstallTheListenersOnceAndHandOverItsDotNetReference()
    {
        var jsRuntime = new RecordingJsRuntime();
        var pageVisibility = new BitPageVisibility(jsRuntime);

        await pageVisibility.Init();
        await pageVisibility.Init();

        Assert.AreEqual(1, jsRuntime.CountOf("BitBlazorUI.PageVisibility.init"),
            "the JS side installs one set of page-wide listeners; initializing twice would double every event");

        var init = jsRuntime.Invocations.Single(i => i.Identifier == "BitBlazorUI.PageVisibility.init");
        Assert.IsTrue(init.Args!.OfType<DotNetObjectReference<BitPageVisibility>>().Any(),
            "the browser is given a reference to call back into");
    }

    [TestMethod]
    public async Task DisposeAsyncShouldRemoveTheListenersAndReleaseTheReference()
    {
        var jsRuntime = new RecordingJsRuntime();
        var pageVisibility = new BitPageVisibility(jsRuntime);
        await pageVisibility.Init();

        await pageVisibility.DisposeAsync();

        Assert.AreEqual(1, jsRuntime.CountOf("BitBlazorUI.PageVisibility.dispose"));
        AssertDotNetReferenceReleased(pageVisibility);
    }

    [TestMethod]
    public async Task DisposeShouldRemoveTheListenersAndReleaseTheReference()
    {
        // The synchronous path cannot await the JS teardown, so it starts it and lets it finish on its own.
        // Against a runtime that answers synchronously - WebAssembly's, and this fake - it is already done by
        // the time Dispose returns.
        var jsRuntime = new RecordingJsRuntime();
        var pageVisibility = new BitPageVisibility(jsRuntime);
        await pageVisibility.Init();

        pageVisibility.Dispose();

        Assert.AreEqual(1, jsRuntime.CountOf("BitBlazorUI.PageVisibility.dispose"));
        AssertDotNetReferenceReleased(pageVisibility);
    }

    [TestMethod]
    public async Task DisposingTwiceShouldTearDownOnlyOnce()
    {
        var jsRuntime = new RecordingJsRuntime();
        var pageVisibility = new BitPageVisibility(jsRuntime);
        await pageVisibility.Init();

        await pageVisibility.DisposeAsync();
        await pageVisibility.DisposeAsync();
        pageVisibility.Dispose();

        Assert.AreEqual(1, jsRuntime.CountOf("BitBlazorUI.PageVisibility.dispose"));
    }

    [TestMethod]
    public async Task DisposingWithoutInitShouldNotReachJavaScript()
    {
        // Nothing was installed, so there is nothing to remove - and a scoped service is disposed whether the
        // app ever used it or not.
        var jsRuntime = new RecordingJsRuntime();
        var pageVisibility = new BitPageVisibility(jsRuntime);

        await pageVisibility.DisposeAsync();
        pageVisibility.Dispose();

        Assert.AreEqual(0, jsRuntime.Invocations.Count);
    }

    [TestMethod]
    public async Task DisposeShouldSwallowAWholeFailingTeardown()
    {
        // Disposal runs from the DI scope's teardown, where throwing would abort the disposal of everything
        // else in the scope. Every failure here means the same thing: the listeners can no longer be reached.
        var jsRuntime = new RecordingJsRuntime();
        var pageVisibility = new BitPageVisibility(jsRuntime);
        await pageVisibility.Init();

        jsRuntime.ExceptionFactory = _ => new JSDisconnectedException("circuit gone");

        await pageVisibility.DisposeAsync();

        // The reference is released in the finally, so a failed JS teardown still cannot leak it.
        AssertDotNetReferenceReleased(pageVisibility);
    }

    [TestMethod]
    public async Task ANewInstanceShouldBeAbleToInitAfterAPreviousOneWasDisposed()
    {
        // The JS dispose resets the JS-side init guard, so a second DI scope - a Blazor Server circuit
        // reconnecting, say - gets its listeners back rather than a silently inert utility.
        var jsRuntime = new RecordingJsRuntime();

        var first = new BitPageVisibility(jsRuntime);
        await first.Init();
        await first.DisposeAsync();

        var second = new BitPageVisibility(jsRuntime);
        await second.Init();

        Assert.AreEqual(2, jsRuntime.CountOf("BitBlazorUI.PageVisibility.init"));
        Assert.AreEqual(1, jsRuntime.CountOf("BitBlazorUI.PageVisibility.dispose"));
    }

    [TestMethod]
    public async Task TheCallbacksShouldRaiseTheEventsTheyStandFor()
    {
        var pageVisibility = new BitPageVisibility(new RecordingJsRuntime());

        bool? hidden = null;
        bool? blurred = null;
        pageVisibility.OnChange += value => { hidden = value; return Task.CompletedTask; };
        pageVisibility.OnWindowFocusChange += value => { blurred = value; return Task.CompletedTask; };

        await pageVisibility._VisibilityChanged(true);
        await pageVisibility._WindowFocusChanged(true);

        Assert.IsTrue(hidden);
        Assert.IsTrue(blurred);

        await pageVisibility._VisibilityChanged(false);
        await pageVisibility._WindowFocusChanged(false);

        Assert.IsFalse(hidden);
        Assert.IsFalse(blurred);
    }

    [TestMethod]
    public async Task TheCallbacksShouldDoNothingWhenNobodyIsListening()
    {
        // The browser keeps calling these for as long as the listeners are installed, including before an
        // app has subscribed and after it has unsubscribed.
        var pageVisibility = new BitPageVisibility(new RecordingJsRuntime());

        await pageVisibility._VisibilityChanged(true);
        await pageVisibility._WindowFocusChanged(true);
    }

    [TestMethod]
    public void TheJsInvokableNamesShouldBeTheOnesTheScriptCallsBack()
    {
        // Nothing but these two strings ties the browser's callbacks to this type: a rename on either side
        // compiles, ships, and then silently reports nothing.
        var blazorUiRoot = JsInteropSources.TryFindBlazorUiRoot();
        if (blazorUiRoot is null)
        {
            Assert.Inconclusive("Skipped: could not locate the BlazorUI source root.");
            return;
        }

        var script = File.ReadAllText(Path.Combine(blazorUiRoot, "Bit.BlazorUI", "Scripts", "PageVisibility.ts"));

        foreach (var name in typeof(BitPageVisibility)
                     .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                     .Select(m => m.GetCustomAttribute<JSInvokableAttribute>()?.Identifier)
                     .Where(n => n is not null))
        {
            StringAssert.Contains(script, $"'{name}'",
                $"PageVisibility.ts never calls back into '{name}', so that callback can never fire.");
        }

        StringAssert.Contains(script, "static dispose(",
            "BitPageVisibility's teardown calls BitBlazorUI.PageVisibility.dispose, which has to exist.");
    }

    private static void AssertDotNetReferenceReleased(BitPageVisibility pageVisibility)
    {
        var field = typeof(BitPageVisibility).GetField("_dotnetObj", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(field, "the reference field was renamed; this test can no longer see whether it is released");

        var reference = (DotNetObjectReference<BitPageVisibility>?)field.GetValue(pageVisibility);
        Assert.IsNotNull(reference);

        // A released DotNetObjectReference throws when its target is read - which is what says the browser
        // can no longer call back into a disposed utility.
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = reference.Value);
    }


    private sealed record Invocation(string Identifier, object?[]? Args);

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public List<Invocation> Invocations { get; } = [];

        public Func<string, Exception>? ExceptionFactory { get; set; }

        public int CountOf(string identifier) => Invocations.Count(i => i.Identifier == identifier);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Invocations.Add(new Invocation(identifier, args));

            if (ExceptionFactory is not null) throw ExceptionFactory(identifier);

            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }
}
