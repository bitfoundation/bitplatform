using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorHeadlessUI;

/// <summary>Rendered inside <see cref="BitBhCommandList"/> when no item matches the search.</summary>
public class BitBhCommandEmpty : BitBhComponentBase, IDisposable
{
    [CascadingParameter]
    private BitBhCommandContext? CommandContext { get; set; }

    /// <summary>The empty-state content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => CommandContext?.Subscribe(StateHasChanged);

    public void Dispose() => CommandContext?.Unsubscribe(StateHasChanged);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CommandContext is null || CommandContext.VisibleCount > 0) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddContent(20, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "presentation";
        attrs["data-bit-bh-command-empty"] = "";
        return attrs;
    }
}
