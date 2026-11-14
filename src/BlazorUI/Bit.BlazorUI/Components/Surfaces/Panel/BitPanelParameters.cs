namespace Bit.BlazorUI;

/// <summary>
/// The parameters of a panel shown by the <see cref="BitPanelService"/>: the parameters of <see cref="BitPanel"/> that a
/// panel shown from code is given, and the close guard and navigation policy the service applies to it. Every one left
/// unset falls back to the one of the <see cref="BitPanelContainer"/>, and then to the default of <see cref="BitPanel"/>.
/// </summary>
public class BitPanelParameters
{
    /// <summary>
    /// Whether the panel is disabled.
    /// </summary>
    public bool? Disabled { get; set; }

    /// <summary>
    /// The HTML attributes splatted on the root of the panel.
    /// </summary>
    public Dictionary<string, object> HtmlAttributes { get; set; } = [];

    /// <summary>
    /// The aria-label of the panel.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Takes the scrolling of the page away while the panel is open.
    /// </summary>
    public bool? AutoToggleScroll { get; set; }

    /// <summary>
    /// Keeps a click on the overlay from dismissing the panel.
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    /// The content of the body of a panel that has a header, a footer or a close button, in place of the content the
    /// panel is shown with.
    /// </summary>
    public RenderFragment? Body { get; set; }

    /// <summary>
    /// The guard a dismissal of the panel - by the user, or through <c>TryClose</c> - is put to: answering false keeps
    /// the panel open. Closing it from code with <c>Close</c> is never put to it.
    /// </summary>
    public Func<Task<bool>>? CanClose { get; set; }

    /// <summary>
    /// The CSS class of the root of the panel.
    /// </summary>
    public string? Class { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the panel.
    /// </summary>
    public BitPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// The name of the close button of the panel.
    /// </summary>
    public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// The icon of the close button, provided as custom CSS classes of an external icon library.
    /// </summary>
    public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the close button, from the built-in Fluent UI icons.
    /// </summary>
    public string? CloseIconName { get; set; }

    /// <summary>
    /// Whether the panel is closed when the app navigates to another page. Default: true; a persistent panel is never
    /// closed by a navigation.
    /// </summary>
    public bool? CloseOnNavigation { get; set; }

    /// <summary>
    /// The direction of the panel.
    /// </summary>
    public BitDir? Dir { get; set; }

    /// <summary>
    /// The footer of the panel.
    /// </summary>
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The text of the footer of the panel.
    /// </summary>
    public string? FooterText { get; set; }

    /// <summary>
    /// Stretches the panel over the whole screen.
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// The name of the grabber of a bottom sheet with more than one snap point, as a composite format string.
    /// </summary>
    public string? GrabberAriaLabelFormat { get; set; }

    /// <summary>
    /// The header of the panel.
    /// </summary>
    public RenderFragment? Header { get; set; }

    /// <summary>
    /// The text of the header of the panel.
    /// </summary>
    public string? HeaderText { get; set; }

    /// <summary>
    /// Reports the panel as an alertdialog instead of a dialog.
    /// </summary>
    public bool? IsAlert { get; set; }

    /// <summary>
    /// Darkens the page behind the panel with its overlay.
    /// </summary>
    public bool? ModeFull { get; set; }

    /// <summary>
    /// Leaves the page behind the panel usable while it is open.
    /// </summary>
    public bool? Modeless { get; set; }

    /// <summary>
    /// Leaves the focus where it is when the panel opens.
    /// </summary>
    public bool? NoAutoFocus { get; set; }

    /// <summary>
    /// Keeps the Escape key from dismissing the panel.
    /// </summary>
    public bool? NoDismissOnEscape { get; set; }

    /// <summary>
    /// Lets the keyboard leave the panel while it is open.
    /// </summary>
    public bool? NoFocusTrap { get; set; }

    /// <summary>
    /// Leaves the focus where it is when the panel closes.
    /// </summary>
    public bool? NoRestoreFocus { get; set; }

    /// <summary>
    /// Leaves the scrolling of the page alone while the panel is open.
    /// </summary>
    public bool? NoScrollLock { get; set; }

    /// <summary>
    /// Turns the swipe gesture that dismisses the panel off.
    /// </summary>
    public bool? NoSwipe { get; set; }

    /// <summary>
    /// The callback for when the panel is dismissed by the user.
    /// </summary>
    public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// The callback for when the Escape key is pressed in the panel.
    /// </summary>
    public EventCallback<KeyboardEventArgs> OnEscapeKeyDown { get; set; }

    /// <summary>
    /// The callback for when the panel has opened.
    /// </summary>
    public EventCallback OnOpen { get; set; }

    /// <summary>
    /// The callback for when the overlay of the panel is clicked.
    /// </summary>
    public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// The edge of the screen the panel slides in from.
    /// </summary>
    public BitPlacement? Placement { get; set; }

    /// <summary>
    /// The ARIA role of the panel, in place of the dialog it is announced as.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// The CSS selector of the scroller whose scrolling is taken away while the panel is open.
    /// </summary>
    public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the panel.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// The size of the panel in pixels along the axis it slides on.
    /// </summary>
    public double? Size { get; set; }

    /// <summary>
    /// The snap point a bottom sheet opens at. The sheet keeps the one the user moves it to from there on.
    /// </summary>
    public int? SnapPoint { get; set; }

    /// <summary>
    /// The heights a panel at the bottom of the screen settles at - a bottom sheet.
    /// </summary>
    public IReadOnlyList<double>? SnapPoints { get; set; }

    /// <summary>
    /// The CSS style of the root of the panel.
    /// </summary>
    public string? Style { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the panel.
    /// </summary>
    public BitPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// The id of the element that describes the panel.
    /// </summary>
    public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// How far the panel has to be dragged before it is dismissed, as a fraction of its own size.
    /// </summary>
    public decimal? SwipeTrigger { get; set; }

    /// <summary>
    /// The id of the element that names the panel.
    /// </summary>
    public string? TitleAriaId { get; set; }

    /// <summary>
    /// The visibility of the panel.
    /// </summary>
    public BitVisibility? Visibility { get; set; }



    internal BitPanelParameters Copy()
    {
        var copy = (BitPanelParameters)MemberwiseClone();
        copy.HtmlAttributes = new(HtmlAttributes ?? []);
        copy.Classes = BitPanelClassStyles.Merge(Classes, null);
        copy.Styles = BitPanelClassStyles.Merge(Styles, null);
        return copy;
    }

    /// <summary>
    /// Merges two sets of parameters, the first one winning where both set a parameter; the callbacks of both are
    /// invoked, the first one's first. Null when both are.
    /// </summary>
    public static BitPanelParameters? Merge(BitPanelParameters? params1, BitPanelParameters? params2)
    {
        if (params1 is null && params2 is null) return null;
        if (params2 is null) return params1;
        if (params1 is null) return params2;

        return new BitPanelParameters
        {
            Disabled = params1.Disabled ?? params2.Disabled,
            HtmlAttributes = BitModalParameters.MergeHtmlAttributes(params2.HtmlAttributes, params1.HtmlAttributes),
            AriaLabel = params1.AriaLabel ?? params2.AriaLabel,
            AutoToggleScroll = params1.AutoToggleScroll ?? params2.AutoToggleScroll,
            Blocking = params1.Blocking ?? params2.Blocking,
            Body = params1.Body ?? params2.Body,
            CanClose = params1.CanClose ?? params2.CanClose,
            Class = params1.Class ?? params2.Class,
            Classes = BitPanelClassStyles.Merge(params1.Classes, params2.Classes),
            CloseButtonTitle = params1.CloseButtonTitle ?? params2.CloseButtonTitle,
            CloseIcon = params1.CloseIcon ?? params2.CloseIcon,
            CloseIconName = params1.CloseIconName ?? params2.CloseIconName,
            CloseOnNavigation = params1.CloseOnNavigation ?? params2.CloseOnNavigation,
            Dir = params1.Dir ?? params2.Dir,
            Footer = params1.Footer ?? params2.Footer,
            FooterText = params1.FooterText ?? params2.FooterText,
            FullSize = params1.FullSize ?? params2.FullSize,
            GrabberAriaLabelFormat = params1.GrabberAriaLabelFormat ?? params2.GrabberAriaLabelFormat,
            Header = params1.Header ?? params2.Header,
            HeaderText = params1.HeaderText ?? params2.HeaderText,
            IsAlert = params1.IsAlert ?? params2.IsAlert,
            ModeFull = params1.ModeFull ?? params2.ModeFull,
            Modeless = params1.Modeless ?? params2.Modeless,
            NoAutoFocus = params1.NoAutoFocus ?? params2.NoAutoFocus,
            NoDismissOnEscape = params1.NoDismissOnEscape ?? params2.NoDismissOnEscape,
            NoFocusTrap = params1.NoFocusTrap ?? params2.NoFocusTrap,
            NoRestoreFocus = params1.NoRestoreFocus ?? params2.NoRestoreFocus,
            NoScrollLock = params1.NoScrollLock ?? params2.NoScrollLock,
            NoSwipe = params1.NoSwipe ?? params2.NoSwipe,
            OnDismiss = MergeCallbacks(params1.OnDismiss, params2.OnDismiss),
            OnEscapeKeyDown = MergeCallbacks(params1.OnEscapeKeyDown, params2.OnEscapeKeyDown),
            OnOpen = MergeCallbacks(params1.OnOpen, params2.OnOpen),
            OnOverlayClick = MergeCallbacks(params1.OnOverlayClick, params2.OnOverlayClick),
            Placement = params1.Placement ?? params2.Placement,
            Role = params1.Role ?? params2.Role,
            ScrollerSelector = params1.ScrollerSelector ?? params2.ScrollerSelector,
            ShowCloseButton = params1.ShowCloseButton ?? params2.ShowCloseButton,
            Size = params1.Size ?? params2.Size,
            SnapPoint = params1.SnapPoint ?? params2.SnapPoint,
            SnapPoints = params1.SnapPoints ?? params2.SnapPoints,
            Style = params1.Style ?? params2.Style,
            Styles = BitPanelClassStyles.Merge(params1.Styles, params2.Styles),
            SubtitleAriaId = params1.SubtitleAriaId ?? params2.SubtitleAriaId,
            SwipeTrigger = params1.SwipeTrigger ?? params2.SwipeTrigger,
            TitleAriaId = params1.TitleAriaId ?? params2.TitleAriaId,
            Visibility = params1.Visibility ?? params2.Visibility,
        };
    }

    private static EventCallback<T> MergeCallbacks<T>(EventCallback<T> callback1, EventCallback<T> callback2)
    {
        if (callback1.HasDelegate is false && callback2.HasDelegate is false) return default;

        // Invoked by the panel's host, never bound to a child component, so the receiver only has to be non-null for
        // the callback to have a delegate.
        return EventCallback.Factory.Create<T>(new object(), async (T e) =>
        {
            await callback1.InvokeAsync(e);
            await callback2.InvokeAsync(e);
        });
    }

    private static EventCallback MergeCallbacks(EventCallback callback1, EventCallback callback2)
    {
        if (callback1.HasDelegate is false && callback2.HasDelegate is false) return default;

        return EventCallback.Factory.Create(new object(), async () =>
        {
            await callback1.InvokeAsync();
            await callback2.InvokeAsync();
        });
    }
}
