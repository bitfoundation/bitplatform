using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.Bheadless;

/// <summary>
/// Turns on F6 / Shift+F6 navigation between the page's landmarks - header, navigation, main, complementary, footer,
/// search and named regions and forms - as React Aria's <c>useLandmark</c> and desktop applications do: keyboard users
/// jump between the areas of a page instead of tabbing through every link on the way. A landmark gets back the element
/// that last had the focus in it; one never visited is focused itself. Renders nothing; place it once (in the layout).
///
/// <code>
/// &lt;BitBhLandmarkNavigation /&gt;
/// </code>
/// </summary>
public sealed class BitBhLandmarkNavigation : BitBhComponentBase, IAsyncDisposable
{
    private bool _enabled;

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _enabled = true;
        await Interop.LandmarksEnableAsync();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_enabled) await Interop.LandmarksDisableAsync();
        _enabled = false;
    }
}
