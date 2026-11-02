using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Samples.Core.Shared;

public partial class HostFacts
{
    /// <summary>More facts, as dt/dd pairs.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string FrameLoop => Capabilities.IsPrerendering
        ? "not yet: nothing runs while prerendering - the enter animations play in CSS until a runtime takes over"
        : Capabilities.SupportsFrameLoop
            ? "yes: interop is synchronous, so the engine can step an animation in C# every frame"
            : RendererInfo.Name == "WebView"
                ? "no: the WebView's interop is asynchronous, so it plays the animations the app hands it - with no network in between"
                : "no: the circuit's interop is asynchronous, so the browser plays the animations the server hands it";
}
