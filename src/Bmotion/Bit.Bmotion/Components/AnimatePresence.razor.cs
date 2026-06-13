using Bit.Bmotion.Context;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Components;

/// <summary>
/// Wraps content that should animate in and out.
/// When <see cref="IsPresent"/> switches from <c>true</c> to <c>false</c>, children
/// are kept in the DOM until their <c>Exit</c> animations finish.
///
/// <example>
/// <code>
/// &lt;AnimatePresence IsPresent="@_visible"&gt;
///     &lt;Motion Tag="div" Animate="..." Exit="..." /&gt;
/// &lt;/AnimatePresence&gt;
/// </code>
/// </example>
/// </summary>
public partial class AnimatePresence : ComponentBase
{
    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>
    /// Controls whether children are present. Setting to <c>false</c> triggers exit
    /// animations before removing children from the DOM.
    /// </summary>
    [Parameter] public bool IsPresent { get; set; } = true;

    /// <summary>
    /// When true, a new set of children waits for the exiting children to finish
    /// before entering. Mirrors Framer Motion's <c>exitBeforeEnter</c>.
    /// </summary>
    [Parameter] public bool ExitBeforeEnter { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ── Internal state ────────────────────────────────────────────────────────

    private readonly PresenceContext _presenceCtx = new();
    private bool _shouldRender = true;
    private bool _prevIsPresent = true;

    // ═══════════════════════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════════════════════

    protected override void OnInitialized()
    {
        _presenceCtx.AllExitsComplete += OnAllExitsComplete;
    }

    protected override void OnParametersSet()
    {
        if (_prevIsPresent && !IsPresent)
        {
            // Children are leaving - signal exiting state so Motion components play Exit
            _presenceCtx.IsExiting = true;
            _shouldRender = true; // keep rendering until exit completes
        }
        else if (!_prevIsPresent && IsPresent)
        {
            // Children are re-entering
            _presenceCtx.IsExiting = false;
            _presenceCtx.Reset();
            _shouldRender = true;
        }

        _prevIsPresent = IsPresent;
    }

    private void OnAllExitsComplete()
    {
        _shouldRender = false;
        _presenceCtx.IsExiting = false;
        InvokeAsync(StateHasChanged);
    }
}
