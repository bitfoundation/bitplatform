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
        var js = new PageStateJsRuntime();
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
        var js = new PageStateJsRuntime();
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
        var js = new PageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var failed = visibility.Init();
        js.Fail(new JSException("BitBlazorUI.PageVisibility is not defined"));

        await Assert.ThrowsExactlyAsync<JSException>(() => failed);

        var retried = visibility.Init();
        js.Answer("""{"hidden":true,"blurred":false}""");
        await retried;

        Assert.AreEqual(2, js.InitCount);
        Assert.IsTrue(visibility.IsHidden);
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
        var js = new PageStateJsRuntime();
        var visibility = new BitPageVisibility(js);

        var init = visibility.Init();
        js.Answer("""{"hidden":false,"blurred":false}""");
        await init;

        await visibility.DisposeAsync();

        Assert.AreEqual(1, js.DisposeCount);
    }



    // Answers the init call when the test says so, the way the browser answers it after a round trip.
    private sealed class PageStateJsRuntime : IJSRuntime
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

            InitCount++;

            return new ValueTask<TValue>(ReadAsync<TValue>(_pending.Task));
        }

        private static async Task<TValue> ReadAsync<TValue>(Task<string> json)
        {
            return JsonSerializer.Deserialize<TValue>(await json, JsonSerializerOptions.Web)!;
        }
    }
}
