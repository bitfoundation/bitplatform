using System;
using System.Linq;
using System.Reflection;
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
/// <remarks>
/// The render held up is the first one after the press has started its hold, not merely the first one after the
/// press: a press that awaits anything before its step renders in between, and a release queued behind that render
/// would be handled before the hold is even asked for - which a different guard covers, leaving this one untested.
/// </remarks>
internal sealed class SpinButtonSlowRender : IDisposable
{
    private readonly ManualResetEventSlim _rendering = new();
    private readonly ManualResetEventSlim _released = new();
    private volatile object? _armedFor;

    /// <summary>
    /// The content of a template the component renders, which holds up the first render after the press has started
    /// its hold until the release has been queued.
    /// </summary>
    public RenderFragment Content => builder =>
    {
        var component = _armedFor;

        if (component is not null && IsHolding(component))
        {
            _armedFor = null;
            _rendering.Set();
            _released.Wait(TimeSpan.FromSeconds(10));
        }

        builder.AddContent(0, "Slow");
    };

    /// <summary>
    /// Presses the button of the component and releases it while the render that follows the start of the hold is
    /// still running, after the given press-and-hold delay has run out.
    /// </summary>
    public async Task ClickAsync(IComponent component, IElement button, int continuousSpinDelay)
    {
        _armedFor = component;

        // The press runs on a thread of its own, so the release can be queued while its render is still going.
        var press = Task.Run(() => button.TriggerEventAsync("onpointerdown", new PointerEventArgs()));

        Assert.IsTrue(_rendering.Wait(TimeSpan.FromSeconds(10)), "The press never rendered with its hold started.");

        // Well past the delay, so the press-and-hold delay that started with the hold has run out by now.
        await Task.Delay(continuousSpinDelay * 4);

        var release = button.TriggerEventAsync("onpointerup", new PointerEventArgs());

        _released.Set();

        await Task.WhenAll(press, release);

        // Long enough for a continuous spin that did start to have stepped again.
        await Task.Delay(continuousSpinDelay * 4);
    }

    public void Dispose()
    {
        _rendering.Dispose();
        _released.Dispose();
    }

    // The press of a spin button is tracked by the internal BitSpinPress every component with spin buttons holds.
    private static bool IsHolding(object component)
    {
        for (var type = component.GetType(); type is not null; type = type.BaseType)
        {
            var field = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                            .FirstOrDefault(f => f.FieldType.Name == "BitSpinPress");

            if (field is null) continue;

            var press = field.GetValue(component)!;

            return (bool)press.GetType().GetProperty("IsHolding")!.GetValue(press)!;
        }

        throw new InvalidOperationException($"{component.GetType().Name} has no spin press.");
    }
}
