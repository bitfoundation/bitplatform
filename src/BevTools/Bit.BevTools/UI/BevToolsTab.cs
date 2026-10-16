using Microsoft.AspNetCore.Components;

namespace Bit.BevTools;

/// <summary>
/// Built-in tabs snapshot session data in <see cref="ComponentBase.OnParametersSet"/>. Blazor skips
/// <c>SetParametersAsync</c> when a child's direct parameters are unchanged, and these tabs have none, so
/// the host cascades a monotonically increasing <see cref="Revision"/> whenever the open tab should re-read.
/// </summary>
public abstract class BevToolsTab : ComponentBase
{
    [CascadingParameter]
    public BevToolsPanel Host { get; set; } = default!;

    [CascadingParameter(Name = "bev-rev")]
    public long Revision { get; set; }

    protected BevToolsSession Session => Host.Session;

    protected BevToolsUiState Ui => Host.Ui;
}
