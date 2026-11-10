using System;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Inputs;

/// <summary>
/// A click on a spin button of a page that is slow to render. On WebAssembly the render of the step a press takes runs
/// in the browser task of the pointerdown, so the pointerup waits behind it, and a press-and-hold delay that runs out
/// during that render is due before the release is handled - which is when Firefox runs it. The renderer's queue is
/// first in, first out, so here a delay that ran out during the render is handled ahead of the release every time.
/// </summary>
internal sealed class SpinButtonSlowRender : IDisposable
{
    private readonly ManualResetEventSlim _rendering = new();
    private readonly ManualResetEventSlim _released = new();
    private volatile bool _armed;

    /// <summary>
    /// The content of a template the component renders, which holds up the first render after the press until the
    /// release has been queued.
    /// </summary>
    public RenderFragment Content => builder =>
    {
        if (_armed)
        {
            _armed = false;
            _rendering.Set();
            _released.Wait(TimeSpan.FromSeconds(10));
        }

        builder.AddContent(0, "Slow");
    };

    /// <summary>
    /// Presses the button and releases it while the render of the press's step is still running, after the given
    /// press-and-hold delay has run out.
    /// </summary>
    public async Task ClickAsync(IElement button, int continuousSpinDelay)
    {
        _armed = true;

        // The press runs on a thread of its own, so the release can be queued while its render is still going.
        var press = Task.Run(() => button.TriggerEventAsync("onpointerdown", new PointerEventArgs()));

        Assert.IsTrue(_rendering.Wait(TimeSpan.FromSeconds(10)), "The press never rendered its step.");

        // Well past the delay, so a press-and-hold delay that started with the press has run out by now.
        await Task.Delay(continuousSpinDelay * 4);

        var release = button.TriggerEventAsync("onpointerup", new PointerEventArgs());

        _released.Set();

        await Task.WhenAll(press, release);
    }

    public void Dispose()
    {
        _rendering.Dispose();
        _released.Dispose();
    }
}
