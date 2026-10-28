using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// A visual divider between command groups; hidden while searching unless
/// <see cref="AlwaysRender"/>. A listbox may only own options and groups, so the
/// divider is presentational (groups already convey the structure).
/// </summary>
public class BitBhCommandSeparator : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhCommandContext? CommandContext { get; set; }

    /// <summary>Keep the separator while searching.</summary>
    [Parameter]
    public bool AlwaysRender { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => CommandContext?.Subscribe(StateHasChanged);

    public void Dispose() => CommandContext?.Unsubscribe(StateHasChanged);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!AlwaysRender && !string.IsNullOrWhiteSpace(CommandContext?.Search)) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "none";
        attrs["aria-hidden"] = "true";
        attrs["data-bit-bh-command-separator"] = "";
        return attrs;
    }
}
