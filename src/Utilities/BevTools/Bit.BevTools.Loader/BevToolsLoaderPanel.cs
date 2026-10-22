using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BevTools;

/// <summary>
/// Renders <c>BevToolsPanel</c> once BevTools runs in this scope: from the start when it was registered at startup,
/// otherwise once <see cref="BevToolsLoader.OpenAsync"/> has loaded and attached it. Place it once inside an
/// interactive component, normally the layout, where <c>&lt;BevToolsPanel /&gt;</c> would go. It names no Bit.BevTools
/// type, so it can be rendered before the assembly is loaded.
/// </summary>
public sealed class BevToolsLoaderPanel : ComponentBase, IDisposable
{
    [Inject] private BevToolsLoader Loader { get; set; } = default!;

    protected override void OnInitialized() => Loader.Changed += OnLoaderChanged;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Loader.PanelType is { } panel)
        {
            builder.OpenComponent(0, panel);
            builder.CloseComponent();
        }
    }

    private void OnLoaderChanged() => _ = InvokeAsync(StateHasChanged);

    public void Dispose() => Loader.Changed -= OnLoaderChanged;
}
