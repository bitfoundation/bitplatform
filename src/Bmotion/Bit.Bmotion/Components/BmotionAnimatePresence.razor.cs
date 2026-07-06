using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion;
/// <summary>
/// Wraps content that should animate in and out.
/// When <see cref="IsPresent"/> switches from <c>true</c> to <c>false</c>, children
/// are kept in the DOM until their <c>Exit</c> animations finish.
///
/// <para>
/// <b>Limitation:</b> presence is controlled by a single <see cref="IsPresent"/> flag for the
/// whole subtree (all-or-nothing). This does not provide per-item enter/exit tracking for keyed
/// lists the way Framer Motion's keyed <c>AnimatePresence</c> does; wrap individual items in their
/// own <see cref="BmotionAnimatePresence"/> if you need independent exit animations per item.
/// </para>
///
/// <example>
/// <code>
/// &lt;BmotionAnimatePresence IsPresent="@_visible"&gt;
///     &lt;Bmotion Animate="..." Exit="..."&gt;
///         &lt;div class="box" /&gt;
///     &lt;/Bmotion&gt;
/// &lt;/BmotionAnimatePresence&gt;
/// </code>
/// </example>
/// </summary>
public partial class BmotionAnimatePresence : ComponentBase
{
    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>
    /// Controls whether children are present. Setting to <c>false</c> triggers exit
    /// animations before removing children from the DOM.
    /// </summary>
    [Parameter] public bool IsPresent { get; set; } = true;

    /// <summary>
    /// How exit and enter sequence when <see cref="IsPresent"/> toggles rapidly:
    /// <see cref="BmPresenceMode.Wait"/> holds the new children until the exiting ones finish
    /// (motion.dev's <c>mode="wait"</c>); <see cref="BmPresenceMode.PopLayout"/> pops exiting
    /// children out of the layout flow so siblings reflow immediately.
    /// Default: <see cref="BmPresenceMode.Sync"/>.
    /// </summary>
    [Parameter] public BmPresenceMode Mode { get; set; }

    /// <summary>Fires when all exiting children have finished their exit animations.</summary>
    [Parameter] public EventCallback OnExitComplete { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }

    // ── Internal state ────────────────────────────────────────────────────────

    private readonly BmotionPresenceContext _presenceCtx = new();
    // Starts false so an initial IsPresent=false renders nothing (children stay unmounted) rather
    // than mounting them; the OnParametersSet transitions flip it on when content should appear.
    private bool _shouldRender;
    // Starts false so an initial IsPresent=false is treated as "nothing was present yet"
    // rather than a present→absent exit transition.
    private bool _prevIsPresent;
    private bool _deferEnter;

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
            // A fresh leave invalidates any pending deferred enter; clear it so stale
            // deferred-enter state can't remount the children after this exit completes.
            _deferEnter = false;

            if (_presenceCtx.ChildCount > 0)
            {
                // Children are leaving - signal exiting state so Bmotion components play Exit
                _presenceCtx.PopLayout = Mode == BmPresenceMode.PopLayout;
                _presenceCtx.IsExiting = true;
                _shouldRender = true; // keep rendering until exit completes
            }
            else
            {
                // No animatable children registered: AllExitsComplete would never fire, so
                // flagging IsExiting/keeping _shouldRender true would strand the content. Drop it now.
                _presenceCtx.IsExiting = false;
                _shouldRender = false;
            }
        }
        else if (!_prevIsPresent && IsPresent)
        {
            if (Mode == BmPresenceMode.Wait && _presenceCtx.IsExiting)
            {
                // Wait for the exiting children to finish before rendering the new ones.
                _deferEnter = true;
            }
            else
            {
                // Children are re-entering
                _presenceCtx.IsExiting = false;
                _presenceCtx.Reset();
                _shouldRender = true;
            }
        }

        _prevIsPresent = IsPresent;
    }

    private void OnAllExitsComplete()
    {
        // Ignore stale callbacks that arrive after a re-entry / reset.
        if (!_presenceCtx.IsExiting) return;

        _presenceCtx.IsExiting = false;

        if (_deferEnter)
        {
            // Deferred re-entry: now that exits are done, render the new children.
            _deferEnter = false;
            _presenceCtx.Reset();
            _shouldRender = true;
        }
        else
        {
            _shouldRender = false;
        }

        InvokeAsync(async () =>
        {
            await OnExitComplete.InvokeAsync();
            StateHasChanged();
        });
    }
}
