using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils;

[TestClass]
public sealed class BitPageVisibilityTests
{
    [TestMethod]
    public async Task BitPageVisibilityShouldReadTheStateThePageIsAlreadyIn()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var init = visibility.Init();
        js.Answer("""{"hidden":true,"blurred":true}""");
        await init;

        Assert.IsTrue(visibility.IsHidden);
        Assert.IsTrue(visibility.IsWindowBlurred);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldNotLetTheInitialStateOverwriteALaterChange()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var init = visibility.Init();

        // The listeners are live as soon as the script registered them, so a change can be reported before the
        // answer to the init call is read.
        await visibility._VisibilityChanged(true);
        await visibility._WindowFocusChanged(true);

        js.Answer("""{"hidden":false,"blurred":false}""");
        await init;

        Assert.IsTrue(visibility.IsHidden);
        Assert.IsTrue(visibility.IsWindowBlurred);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldLetALaterCallRetryAFailedInit()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var failed = visibility.Init();
        var waiting = visibility.Init();
        js.Fail(new JSException("BitBlazorUI.PageVisibility is not defined"));

        // The failure is answered rather than thrown, to the caller that asked and to the one that waited alike.
        Assert.IsFalse(await failed);
        Assert.IsFalse(await waiting);
        Assert.AreEqual(1, js.InitCount);

        var retried = visibility.Init();
        js.Answer("""{"hidden":true,"blurred":false}""");

        Assert.IsTrue(await retried);
        Assert.AreEqual(2, js.InitCount);
        Assert.IsTrue(visibility.IsHidden);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldRaiseAStateOtherThanVisibleAndFocusedThatItReads()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        bool? hidden = null;
        bool? blurred = null;
        visibility.OnChange += value => { hidden = value; return Task.CompletedTask; };
        visibility.OnWindowFocusChange += value => { blurred = value; return Task.CompletedTask; };

        var init = visibility.Init();
        js.Answer("""{"hidden":true,"blurred":false}""");
        await init;

        // A subscriber took the page to be visible and focused until the answer came, so only what differs is raised.
        Assert.IsTrue(hidden);
        Assert.IsNull(blurred);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldRetryAFailedInitOnItsOwn()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var hidden = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        visibility.OnChange += value => { hidden.TrySetResult(value); return Task.CompletedTask; };

        var failed = visibility.Init();
        js.Fail(new JSException("BitBlazorUI.PageVisibility is not defined"));

        Assert.IsFalse(await failed);

        // Nobody calls Init again: the utility asks the page a moment later, and raises what it then reads.
        await WaitUntilAsync(() => js.InitCount == 2);
        js.Answer("""{"hidden":true,"blurred":false}""");

        Assert.IsTrue(await hidden.Task.WaitAsync(TimeSpan.FromSeconds(5)));
        Assert.IsTrue(visibility.IsHidden);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldNotKeepAFailureOfAnyKind()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var failed = visibility.Init();
        js.Fail(new InvalidOperationException("an unexpected failure"));

        Assert.IsFalse(await failed);

        var retried = visibility.Init();
        js.Answer("""{"hidden":false,"blurred":false}""");

        Assert.IsTrue(await retried);
        Assert.AreEqual(2, js.InitCount);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldNotTakeASkippedCallForAnAnswer()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        // The bit Invoke answers with nothing, without calling the browser, while the runtime cannot be used.
        var skipped = visibility.Init();
        js.Answer("null");

        Assert.IsFalse(await skipped);

        var retried = visibility.Init();
        js.Answer("""{"hidden":false,"blurred":false}""");

        Assert.IsTrue(await retried);
        Assert.AreEqual(2, js.InitCount);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldStopRetryingOnceDisposed()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var failed = visibility.Init();
        js.Fail(new JSException("BitBlazorUI.PageVisibility is not defined"));

        Assert.IsFalse(await failed);

        visibility.Dispose();

        await Task.Delay(1500);

        Assert.AreEqual(1, js.InitCount);
        Assert.IsFalse(await visibility.Init());
        Assert.AreEqual(1, js.InitCount);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldNotAskThePageAgainOnceItHasAnswered()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var init = visibility.Init();
        js.Answer("""{"hidden":false,"blurred":false}""");

        Assert.IsTrue(await init);
        Assert.IsTrue(await visibility.Init());
        Assert.AreEqual(1, js.InitCount);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldHandEverySubscriberTheChangeWhenOneOfThemThrows()
    {
        var visibility = new BitPageVisibility(new TestJsRuntime());

        var received = 0;

        visibility.OnChange += _ => throw new InvalidOperationException("a failing subscriber");
        visibility.OnChange += _ => { received++; return Task.CompletedTask; };

        await visibility._VisibilityChanged(true);

        Assert.AreEqual(1, received);
        Assert.IsTrue(visibility.IsHidden);
    }

    [TestMethod]
    public async Task BitPageVisibilityShouldTakeItselfOutOfTheScriptWhenDisposed()
    {
        var js = new DeferredPageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var init = visibility.Init();
        js.Answer("""{"hidden":false,"blurred":false}""");
        await init;

        await visibility.DisposeAsync();

        Assert.AreEqual(1, js.DisposeCount);
    }



    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);

        while (condition() is false)
        {
            if (DateTime.UtcNow > deadline) Assert.Fail("The condition was not met in time.");

            await Task.Delay(20);
        }
    }



    // Answers the init call when the test says so, the way the browser answers it after a round trip.
    private sealed class DeferredPageStateJsRuntime : IJSRuntime
    {
        private TaskCompletionSource<string> _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int InitCount { get; private set; }
        public int DisposeCount { get; private set; }

        public void Answer(string json) => Next().SetResult(json);

        public void Fail(Exception exception) => Next().SetException(exception);

        private TaskCompletionSource<string> Next()
        {
            var pending = _pending;
            _pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
            return pending;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            if (identifier == "BitBlazorUI.PageVisibility.dispose")
            {
                DisposeCount++;
                return new ValueTask<TValue>(default(TValue)!);
            }

            // The call is bound to the answer before it is counted, so a test that waits for the count answers it.
            var pending = _pending.Task;

            InitCount++;

            return new ValueTask<TValue>(ReadAsync<TValue>(pending));
        }

        private static async Task<TValue> ReadAsync<TValue>(Task<string> json)
        {
            return JsonSerializer.Deserialize<TValue>(await json, JsonSerializerOptions.Web)!;
        }
    }
}
