namespace Bit.BevTools;

/// <summary>Why a component rendered. Determined from Activity context and render-batch analysis; see <see cref="BevToolsRenderSample.CauseDetail"/>.</summary>
public enum BevToolsRenderCause
{
    Unknown,
    Initial,
    ParentRender,
    UiEvent,
    Navigation,
    StateHasChanged,
}
