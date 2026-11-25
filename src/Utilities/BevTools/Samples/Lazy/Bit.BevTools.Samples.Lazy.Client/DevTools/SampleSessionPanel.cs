using Bit.BevTools;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BevTools.Samples.Lazy.Client.DevTools;

/// <summary>
/// A panel that reads the BevTools session: it inherits <see cref="BevToolsTab"/>, which lives in Bit.BevTools. A .razor
/// component is always public, so this one is a C# class, internal, rendering by hand: a public type of this assembly
/// whose base type is in Bit.BevTools would break the router's start before Bit.BevTools.wasm is downloaded.
/// </summary>
internal sealed class SampleSessionPanel : BevToolsTab
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "dl");
        builder.AddAttribute(1, "class", "bev-kv sample-session");
        Row(builder, 2, "Platform", Session.Platform ?? "unknown");
        Row(builder, 10, "Value access", Session.ValueAccess.ToString());
        Row(builder, 20, "Panels the app added", string.Join(", ", Session.Registry.Panels.Select(p => p.Title)));
        builder.CloseElement();
    }

    private static void Row(RenderTreeBuilder builder, int sequence, string name, string value)
    {
        builder.OpenElement(sequence, "dt");
        builder.AddContent(sequence + 1, name);
        builder.CloseElement();
        builder.OpenElement(sequence + 2, "dd");
        builder.AddContent(sequence + 3, value);
        builder.CloseElement();
    }
}
