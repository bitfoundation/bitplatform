namespace Bit.BlazorUI;

/// <summary>
/// The set of parameters used to customize a <see cref="BitModal"/> that is shown using the <see cref="BitModalService"/>.
/// </summary>
/// <remarks>
/// BREAKING CHANGE: the boolean members (<see cref="IsEnabled"/>, <see cref="AriaModal"/>, <see cref="Blocking"/>,
/// <see cref="FullHeight"/>, <see cref="FullWidth"/>, <see cref="ShowOverlay"/>) are nullable (<c>bool?</c>)
/// instead of <c>bool</c>. A <c>null</c> value means "not set" and the corresponding <see cref="BitModal"/> default
/// is used (or the cascaded value, when merged). Code that read these members as non-nullable <c>bool</c> must be updated.
/// </remarks>
public class BitModalParameters
{
    /// <summary>
    /// Whether or not the Modal is enabled. <c>null</c> means not set (defaults to enabled).
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the Modal's parameters.
    /// </summary>
    public Dictionary<string, object> HtmlAttributes { get; set; } = [];

    /// <summary>
    /// When true, the Modal is positioned absolute instead of fixed. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? AbsolutePosition { get; set; }

    /// <summary>
    /// The general directionality of the Modal.
    /// </summary>
    public BitDir? Dir { get; set; }

    /// <summary>
    /// The accessible name of the Modal, for the Modals that have no visible title to point
    /// <see cref="TitleAriaId"/> at.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Whether the Modal should be announced as modal to assistive technologies. <c>null</c> means not set (defaults to <c>true</c>).
    /// </summary>
    public bool? AriaModal { get; set; }

    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Modal. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? AutoToggleScroll { get; set; }

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay). <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    /// The content of the body section of the Modal, the alias of the ChildContent of the Modal.
    /// </summary>
    public RenderFragment? Body { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitModal component.
    /// </summary>
    public BitModalClassStyles? Classes { get; set; }

    /// <summary>
    /// The title (and aria-label) of the close button for accessibility and localization.
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
    /// The CSS selector of the drag element, which is the content of the Modal by default.
    /// </summary>
    public string? DragElementSelector { get; set; }

    /// <summary>
    /// Whether the Modal can be dragged around. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? Draggable { get; set; }

    /// <summary>
    /// The template used to render the footer section of the Modal.
    /// </summary>
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// The text of the footer section of the Modal.
    /// </summary>
    public string? FooterText { get; set; }

    /// <summary>
    /// Makes the Modal height 100% of its parent container. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width and height 100% of its parent container. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The template used to render the header section of the Modal.
    /// </summary>
    public RenderFragment? Header { get; set; }

    /// <summary>
    /// The text of the header section of the Modal.
    /// </summary>
    public string? HeaderText { get; set; }

    /// <summary>
    /// The CSS height of the Modal (any CSS length). <c>null</c> means not set (the Modal is as tall as its content).
    /// </summary>
    public string? Height { get; set; }

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
    /// </summary>
    public bool? IsAlert { get; set; }

    /// <summary>
    /// Keeps the Modal in the page while it is closed instead of building it again the next time it opens. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? KeepMounted { get; set; }

    /// <summary>
    /// The CSS height the Modal is not to grow past (any CSS length). <c>null</c> means not set (the height of the screen is the cap).
    /// </summary>
    public string? MaxHeight { get; set; }

    /// <summary>
    /// The CSS width the Modal is not to grow past (any CSS length). <c>null</c> means not set (the width of the screen is the cap).
    /// </summary>
    public string? MaxWidth { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? ModeFull { get; set; }

    /// <summary>
    /// Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal). <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? Modeless { get; set; }

    /// <summary>
    /// Prevents the Modal from moving the focus into itself when it opens. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? NoAutoFocus { get; set; }

    /// <summary>
    /// Removes the default top border of the Modal. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// Prevents the Modal from being dismissed by pressing the Escape key. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? NoDismissOnEscape { get; set; }

    /// <summary>
    /// Prevents the Modal from keeping the keyboard focus inside itself while it is open. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? NoFocusTrap { get; set; }

    /// <summary>
    /// Prevents the Modal from handing the focus back to the element that had it before the Modal opened. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? NoRestoreFocus { get; set; }

    /// <summary>
    /// Prevents the Modal from holding the page still while it is open. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? NoScrollLock { get; set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    public EventCallback<MouseEventArgs> OnDismiss { get; set; }

    /// <summary>
    /// A callback function for when the Escape key is pressed inside the Modal, including the presses a Modal
    /// with <c>NoDismissOnEscape</c> refuses to be dismissed by.
    /// </summary>
    public EventCallback<KeyboardEventArgs> OnEscapeKeyDown { get; set; }

    /// <summary>
    /// A callback function for when the Modal is opened.
    /// </summary>
    public EventCallback OnOpen { get; set; }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
    public EventCallback<MouseEventArgs> OnOverlayClick { get; set; }

    /// <summary>
    /// Position of the Modal on the screen.
    /// </summary>
    public BitPosition? Position { get; set; }

    /// <summary>
    /// The element reference of the scroller the Modal toggles the overflow of while it is open.
    /// </summary>
    public ElementReference? ScrollerElement { get; set; }

    /// <summary>
    /// The CSS selector of the element whose scrolling the Modal holds while it is open, for the layouts whose
    /// scroller is not the page itself. <c>null</c> means not set (the page is held).
    /// </summary>
    public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the Modal. <c>null</c> means not set (defaults to <c>false</c>).
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Whether the overlay should be rendered. <c>null</c> means not set (defaults to <c>true</c>).
    /// </summary>
    public bool? ShowOverlay { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitModal component.
    /// </summary>
    public BitModalClassStyles? Styles { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Modal, if any.
    /// </summary>
    public string? SubtitleAriaId { get; set; }

    /// <summary>
    /// ARIA id for the title of the Modal, if any.
    /// </summary>
    public string? TitleAriaId { get; set; }

    /// <summary>
    /// The visibility state (visible, hidden, or collapsed) of the Modal.
    /// </summary>
    public BitVisibility? Visibility { get; set; }

    /// <summary>
    /// The CSS width of the Modal (any CSS length). <c>null</c> means not set (the Modal is as wide as its content).
    /// </summary>
    public string? Width { get; set; }


    /// <summary>
    /// Merges two sets of <see cref="BitModalParameters"/> giving precedence to the values of the first one.
    /// </summary>
    public static BitModalParameters? Merge(BitModalParameters? params1, BitModalParameters? params2)
    {
        if (params1 is null && params2 is null) return null;

        if (params2 is null) return params1;
        if (params1 is null) return params2;


        return new BitModalParameters
        {
            IsEnabled = params1.IsEnabled ?? params2.IsEnabled,
            HtmlAttributes = (params2.HtmlAttributes ?? []).Concat(params1.HtmlAttributes ?? []).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Last().Value),
            AbsolutePosition = params1.AbsolutePosition ?? params2.AbsolutePosition,
            Dir = params1.Dir ?? params2.Dir,
            AriaLabel = params1.AriaLabel ?? params2.AriaLabel,
            AriaModal = params1.AriaModal ?? params2.AriaModal,
            AutoToggleScroll = params1.AutoToggleScroll ?? params2.AutoToggleScroll,
            Blocking = params1.Blocking ?? params2.Blocking,
            Body = params1.Body ?? params2.Body,
            Classes = BitModalClassStyles.Merge(params1.Classes, params2.Classes),
            CloseButtonTitle = params1.CloseButtonTitle ?? params2.CloseButtonTitle,
            CloseIcon = params1.CloseIcon ?? params2.CloseIcon,
            CloseIconName = params1.CloseIconName ?? params2.CloseIconName,
            DragElementSelector = params1.DragElementSelector ?? params2.DragElementSelector,
            Draggable = params1.Draggable ?? params2.Draggable,
            Footer = params1.Footer ?? params2.Footer,
            FooterText = params1.FooterText ?? params2.FooterText,
            FullHeight = params1.FullHeight ?? params2.FullHeight,
            FullSize = params1.FullSize ?? params2.FullSize,
            FullWidth = params1.FullWidth ?? params2.FullWidth,
            Header = params1.Header ?? params2.Header,
            HeaderText = params1.HeaderText ?? params2.HeaderText,
            Height = params1.Height ?? params2.Height,
            IsAlert = params1.IsAlert ?? params2.IsAlert,
            KeepMounted = params1.KeepMounted ?? params2.KeepMounted,
            MaxHeight = params1.MaxHeight ?? params2.MaxHeight,
            MaxWidth = params1.MaxWidth ?? params2.MaxWidth,
            ModeFull = params1.ModeFull ?? params2.ModeFull,
            Modeless = params1.Modeless ?? params2.Modeless,
            NoAutoFocus = params1.NoAutoFocus ?? params2.NoAutoFocus,
            NoBorder = params1.NoBorder ?? params2.NoBorder,
            NoDismissOnEscape = params1.NoDismissOnEscape ?? params2.NoDismissOnEscape,
            NoFocusTrap = params1.NoFocusTrap ?? params2.NoFocusTrap,
            NoRestoreFocus = params1.NoRestoreFocus ?? params2.NoRestoreFocus,
            NoScrollLock = params1.NoScrollLock ?? params2.NoScrollLock,
            OnDismiss = MergeCallbacks(params1.OnDismiss, params2.OnDismiss),
            OnEscapeKeyDown = MergeCallbacks(params1.OnEscapeKeyDown, params2.OnEscapeKeyDown),
            OnOpen = MergeCallbacks(params1.OnOpen, params2.OnOpen),
            OnOverlayClick = MergeCallbacks(params1.OnOverlayClick, params2.OnOverlayClick),
            Position = params1.Position ?? params2.Position,
            ScrollerElement = params1.ScrollerElement ?? params2.ScrollerElement,
            ScrollerSelector = params1.ScrollerSelector ?? params2.ScrollerSelector,
            ShowCloseButton = params1.ShowCloseButton ?? params2.ShowCloseButton,
            ShowOverlay = params1.ShowOverlay ?? params2.ShowOverlay,
            Styles = BitModalClassStyles.Merge(params1.Styles, params2.Styles),
            SubtitleAriaId = params1.SubtitleAriaId ?? params2.SubtitleAriaId,
            TitleAriaId = params1.TitleAriaId ?? params2.TitleAriaId,
            Visibility = params1.Visibility ?? params2.Visibility,
            Width = params1.Width ?? params2.Width,
        };
    }

    /// <summary>
    /// Composes two <see cref="EventCallback{TValue}"/> into one that invokes both (first then second).
    /// Returns an empty callback when neither source has a delegate, so the merged result preserves the
    /// "no delegate" semantics (<see cref="EventCallback.HasDelegate"/> stays <c>false</c>) instead of
    /// reporting a handler that does nothing.
    /// </summary>
    private static EventCallback<T> MergeCallbacks<T>(EventCallback<T> callback1, EventCallback<T> callback2)
    {
        if (callback1.HasDelegate is false && callback2.HasDelegate is false) return default;

        // These callbacks are invoked manually (never bound to a child component), so the
        // EventCallback receiver only needs to be non-null to be considered "has delegate".
        // A throwaway object() is sufficient here; there's no component to associate for re-render.
        return EventCallback.Factory.Create<T>(new object(), async (T e) =>
        {
            await callback1.InvokeAsync(e);
            await callback2.InvokeAsync(e);
        });
    }

    /// <summary>
    /// The argument-less counterpart of the composition above, for the callbacks that carry no event data.
    /// </summary>
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
