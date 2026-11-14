using System.Diagnostics.CodeAnalysis;

namespace Bit.BlazorUI;

/// <summary>
/// BitLongPress raises an event when its content is pressed and held: a touch, a pen or a mouse button kept down in
/// place for a moment, or - with <see cref="KeyboardTrigger"/> - the context-menu keys of the keyboard.
/// </summary>
/// <remarks>
/// <para>
/// A long press is what a touch screen has for a secondary action, where a desktop has the right-click. iOS Safari never
/// turns it into a <c>contextmenu</c> event, so a menu that opens on one (<c>BitContextMenu</c>) opens on
/// <see cref="OnLongPress"/> too, at the point it reports.
/// </para>
/// <para>
/// The press is called off when the pointer moves further than <see cref="MoveTolerance"/>, when the page scrolls under
/// it (the browser cancels the pointer to scroll), when a second finger comes down, and when it is released early; the
/// content keeps scrolling and pinch-zooming as before, as nothing here takes a gesture from the browser. A press that
/// was long swallows the click its release would make, so the content under it is not also clicked, and a touch held on
/// the content brings up neither the text selection nor the callout of the browser. While a press is held the root
/// carries the <c>bit-lpr-prs</c> class.
/// </para>
/// <para>
/// A long press is no path, so it needs no alternative under WCAG 2.5.1, but it does need a keyboard one (2.1.1):
/// <see cref="KeyboardTrigger"/> is it.
/// </para>
/// </remarks>
public partial class BitLongPress : BitComponentBase
{
    private int _appliedDelay;
    private decimal _appliedMoveTolerance;
    private bool _appliedTouchOnly;
    private bool _appliedKeyboardTrigger;
    private bool _setupFailed;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Gets or sets the cascading parameters for the long press component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple long press components
    /// through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitLongPressParams.ParamName)]
    public BitLongPressParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content the press is held on.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// The time in milliseconds a press has to be held before it is a long press. Default: 500.
    /// </summary>
    [Parameter] public int? Delay { get; set; }

    /// <summary>
    /// Makes the long press reachable from the keyboard, where the keys that bring up a context menu - Shift+F10 and the
    /// Menu key - raise <see cref="OnLongPress"/>, at the middle of the element that has the focus and with a
    /// <c>PointerType</c> of "keyboard".
    /// </summary>
    /// <remarks>
    /// The root becomes a tab stop (unless a <see cref="BitComponentBase.TabIndex"/> says otherwise) named a group, and the
    /// keys are named to assistive technologies in <c>aria-keyshortcuts</c>. The keys are taken on the root and on any
    /// element in it but a field being typed in, whose own context menu is the editing one. Give the long press an
    /// <see cref="BitComponentBase.AriaLabel"/> that says what it does.
    /// </remarks>
    [Parameter] public bool KeyboardTrigger { get; set; }

    /// <summary>
    /// How far in pixels the pointer may move while it is held before the press is called off. Default: 10.
    /// </summary>
    [Parameter] public decimal? MoveTolerance { get; set; }

    /// <summary>
    /// The callback for when a press has been held for <see cref="Delay"/>, or the context-menu keys of
    /// <see cref="KeyboardTrigger"/> are pressed.
    /// </summary>
    [Parameter] public EventCallback<BitLongPressEventArgs> OnLongPress { get; set; }

    /// <summary>
    /// Leaves the mouse out: only a touch or a pen is held, and a mouse button held down does nothing.
    /// </summary>
    [Parameter] public bool TouchOnly { get; set; }



    [JSInvokable("OnLongPress")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitLongPressEventArgs))]
    public async Task _OnLongPress(double clientX, double clientY, string pointerType)
    {
        if (Disabled) return;
        if (pointerType == "keyboard" && KeyboardTrigger is false) return;

        await OnLongPress.InvokeAsync(new(clientX, clientY, pointerType));
    }



    protected override string RootElementClass => "bit-lpr";

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var delay = Math.Max(Delay ?? 500, 0);
        var moveTolerance = Math.Max(MoveTolerance ?? 10, 0);
        var touchOnly = TouchOnly;
        var keyboardTrigger = KeyboardTrigger;

        if (firstRender ||
            _setupFailed ||
            _appliedDelay != delay ||
            _appliedMoveTolerance != moveTolerance ||
            _appliedTouchOnly != touchOnly ||
            _appliedKeyboardTrigger != keyboardTrigger)
        {
            // What is remembered is what the long press is being set up with, before the calls below are awaited, so a
            // render that lands while they are in flight does not set it up all over again. A setup that failed is
            // marked instead, and the next render tries again.
            _appliedDelay = delay;
            _appliedMoveTolerance = moveTolerance;
            _appliedTouchOnly = touchOnly;
            _appliedKeyboardTrigger = keyboardTrigger;
            _setupFailed = false;

            try
            {
                if (firstRender is false)
                {
                    await _js.BitLongPressDispose(UniqueId);
                }

                // The JS side disposes the .NET reference it is handed when the long press is disposed or set up again,
                // so each setup gets a fresh one. Until the setup call has handed it over, disposing it is this side's.
                var dotnetObj = DotNetObjectReference.Create(this);
                try
                {
                    await _js.BitLongPressSetup(UniqueId, RootElement, delay, moveTolerance, touchOnly, keyboardTrigger, dotnetObj);
                }
                catch
                {
                    dotnetObj.Dispose();
                    throw;
                }
            }
            catch (JSDisconnectedException) // we can ignore this exception here
            {
                _setupFailed = true;
            }
            catch
            {
                _setupFailed = true;
                throw;
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }



    // ARIA prohibits naming an element with no role, so a named long press is a group - the generic container a name
    // can be given to - unless the page gave it a role of its own. One the keyboard can reach is one too: a tab stop
    // with no role is announced as nothing at all.
    private string? _GetRole(string? ariaLabel)
    {
        var role = GetSplattedAttribute("role");
        if (role is not null) return role;

        return ariaLabel.HasValue() || GetSplattedAttribute("aria-labelledby").HasValue() || _IsKeyboardReachable ? "group" : null;
    }

    // The long press is only put in the tab order for the keys it answers to, and only while it answers to them.
    private string? _GetTabIndex()
    {
        return TabIndex ?? GetSplattedAttribute("tabindex") ?? (_IsKeyboardReachable ? "0" : null);
    }

    private string? _GetKeyShortcuts()
    {
        var keyShortcuts = GetSplattedAttribute("aria-keyshortcuts");
        if (keyShortcuts is not null || _IsKeyboardReachable is false) return keyShortcuts;

        return "Shift+F10 ContextMenu";
    }

    private bool _IsKeyboardReachable => KeyboardTrigger && Disabled is false;



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        try
        {
            await _js.BitLongPressDispose(UniqueId);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here

        await base.DisposeAsync(disposing);
    }
}
