using Microsoft.AspNetCore.Components.Rendering;

namespace Bit.BlazorUI;

/// <summary>
/// What the <see cref="BitPanelService"/> renders for one of its panels: a <see cref="BitPanel"/> given the parameters the
/// panel was shown with, merged with the container's, and wired to its reference - open until the service closes it, a
/// dismissal by the user put to the close guard and reported to the service, and the exit animation heard out before the
/// container takes it out of the page.
/// </summary>
/// <remarks>
/// The panel is a plain BitPanel rather than one that reads the service's parameters itself, so a panel shown from code is
/// the very same component as one declared in markup, and nothing about BitPanel has to know a service exists.
/// </remarks>
internal sealed class _BitPanelServiceHost : ComponentBase
{
    // The snap point a bottom sheet is at, which the sheet moves on its own (a drag, its grabber) from the one it was
    // shown at.
    private int? _snapPoint;
    private bool _snapPointTaken;



    [Parameter] public BitPanelReference Reference { get; set; } = default!;

    [Parameter] public RenderFragment? Content { get; set; }

    [CascadingParameter] private BitPanelParameters? Parameters { get; set; }

    [CascadingParameter(Name = BitModalExit.CascadingName)] private BitModalExit? Exit { get; set; }



    protected override void OnParametersSet()
    {
        if (_snapPointTaken is false)
        {
            _snapPointTaken = true;
            _snapPoint = Parameters?.SnapPoint;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var p = Parameters ?? new BitPanelParameters();
        var seq = 0;

        builder.OpenComponent<BitPanel>(seq++);

        foreach (var attribute in p.HtmlAttributes ?? [])
        {
            builder.AddAttribute(seq, attribute.Key, attribute.Value);
        }
        seq++;

        // Open until the service closes it, after which the container keeps it in the page, closed, until it has slid
        // out (BitModalExit).
        builder.AddComponentParameter(seq++, nameof(BitPanel.IsOpen), Reference.IsClosed is false);
        builder.AddComponentParameter(seq++, nameof(BitPanel.IsOpenChanged), EventCallback.Factory.Create<bool>(this, HandleIsOpenChanged));
        builder.AddComponentParameter(seq++, nameof(BitPanel.OnDismissing), EventCallback.Factory.Create<BitPanelDismissArgs>(this, HandleOnDismissing));
        builder.AddComponentParameter(seq++, nameof(BitPanel.OnTransitionEnd), EventCallback.Factory.Create<bool>(this, HandleOnTransitionEnd));
        builder.AddComponentParameter(seq++, nameof(BitPanel.ChildContent), Content);

        Add(builder, ref seq, nameof(BitPanel.Disabled), p.Disabled);
        Add(builder, ref seq, nameof(BitPanel.AriaLabel), p.AriaLabel);
        Add(builder, ref seq, nameof(BitPanel.AutoToggleScroll), p.AutoToggleScroll);
        Add(builder, ref seq, nameof(BitPanel.Blocking), p.Blocking);
        Add(builder, ref seq, nameof(BitPanel.Body), p.Body);
        Add(builder, ref seq, nameof(BitPanel.Class), p.Class);
        Add(builder, ref seq, nameof(BitPanel.Classes), p.Classes);
        Add(builder, ref seq, nameof(BitPanel.CloseButtonTitle), p.CloseButtonTitle);
        Add(builder, ref seq, nameof(BitPanel.CloseIcon), p.CloseIcon);
        Add(builder, ref seq, nameof(BitPanel.CloseIconName), p.CloseIconName);
        Add(builder, ref seq, nameof(BitPanel.Dir), p.Dir);
        Add(builder, ref seq, nameof(BitPanel.Footer), p.Footer);
        Add(builder, ref seq, nameof(BitPanel.FooterText), p.FooterText);
        Add(builder, ref seq, nameof(BitPanel.FullSize), p.FullSize);
        Add(builder, ref seq, nameof(BitPanel.GrabberAriaLabelFormat), p.GrabberAriaLabelFormat);
        Add(builder, ref seq, nameof(BitPanel.Header), p.Header);
        Add(builder, ref seq, nameof(BitPanel.HeaderText), p.HeaderText);
        Add(builder, ref seq, nameof(BitPanel.IsAlert), p.IsAlert);
        Add(builder, ref seq, nameof(BitPanel.ModeFull), p.ModeFull);
        Add(builder, ref seq, nameof(BitPanel.Modeless), p.Modeless);
        Add(builder, ref seq, nameof(BitPanel.NoAutoFocus), p.NoAutoFocus);
        Add(builder, ref seq, nameof(BitPanel.NoDismissOnEscape), p.NoDismissOnEscape);
        Add(builder, ref seq, nameof(BitPanel.NoFocusTrap), p.NoFocusTrap);
        Add(builder, ref seq, nameof(BitPanel.NoRestoreFocus), p.NoRestoreFocus);
        Add(builder, ref seq, nameof(BitPanel.NoScrollLock), p.NoScrollLock);
        Add(builder, ref seq, nameof(BitPanel.NoSwipe), p.NoSwipe);
        Add(builder, ref seq, nameof(BitPanel.Placement), p.Placement);
        Add(builder, ref seq, nameof(BitPanel.Role), p.Role);
        Add(builder, ref seq, nameof(BitPanel.ScrollerSelector), p.ScrollerSelector);
        Add(builder, ref seq, nameof(BitPanel.ShowCloseButton), p.ShowCloseButton);
        Add(builder, ref seq, nameof(BitPanel.Size), p.Size);
        Add(builder, ref seq, nameof(BitPanel.SnapPoints), p.SnapPoints);
        Add(builder, ref seq, nameof(BitPanel.Style), p.Style);
        Add(builder, ref seq, nameof(BitPanel.Styles), p.Styles);
        Add(builder, ref seq, nameof(BitPanel.SubtitleAriaId), p.SubtitleAriaId);
        Add(builder, ref seq, nameof(BitPanel.SwipeTrigger), p.SwipeTrigger);
        Add(builder, ref seq, nameof(BitPanel.TitleAriaId), p.TitleAriaId);
        Add(builder, ref seq, nameof(BitPanel.Visibility), p.Visibility);

        if (p.SnapPoints is { Count: > 0 })
        {
            builder.AddComponentParameter(seq++, nameof(BitPanel.SnapPoint), _snapPoint);
            builder.AddComponentParameter(seq++, nameof(BitPanel.SnapPointChanged), EventCallback.Factory.Create<int?>(this, v => _snapPoint = v));
        }

        if (p.OnDismiss.HasDelegate) builder.AddComponentParameter(seq, nameof(BitPanel.OnDismiss), p.OnDismiss);
        seq++;
        if (p.OnEscapeKeyDown.HasDelegate) builder.AddComponentParameter(seq, nameof(BitPanel.OnEscapeKeyDown), p.OnEscapeKeyDown);
        seq++;
        if (p.OnOpen.HasDelegate) builder.AddComponentParameter(seq, nameof(BitPanel.OnOpen), p.OnOpen);
        seq++;
        if (p.OnOverlayClick.HasDelegate) builder.AddComponentParameter(seq, nameof(BitPanel.OnOverlayClick), p.OnOverlayClick);
        seq++;

        builder.CloseComponent();
    }

    // A parameter the panel was not given is left out rather than written as its default, so BitPanel's own default -
    // and a BitParams cascade around the container - still apply to it.
    private static void Add<T>(RenderTreeBuilder builder, ref int seq, string name, T value)
    {
        if (value is not null) builder.AddComponentParameter(seq, name, value);
        seq++;
    }

    // The user dismissed the panel - the overlay, the Escape key, the close button, a swipe - and it has already taken
    // itself off the screen, its close guard having had its say (HandleOnDismissing). The service hears of it through
    // DismissFromModal, which does not put the guard to the user a second time.
    private async Task HandleIsOpenChanged(bool isOpen)
    {
        if (isOpen || Reference.IsClosed) return;

        await Reference.DismissFromModal();
    }

    private async Task HandleOnDismissing(BitPanelDismissArgs args)
    {
        var canClose = Parameters?.CanClose;
        if (canClose is null) return;

        if (await canClose() is false) args.Cancel = true;
    }

    // The panel is out of the way once it has slid out after the service closed it, and the container can take it out of
    // the page. Should the transition never end - a panel that is not going to render again - the container's own
    // deadline takes it out instead.
    private void HandleOnTransitionEnd(bool isOpen)
    {
        if (isOpen is false && Reference.IsClosed) Exit?.Left();
    }
}
