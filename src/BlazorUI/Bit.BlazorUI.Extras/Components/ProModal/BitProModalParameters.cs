namespace Bit.BlazorUI;

/// <summary>
/// The set of parameters used to customize a <see cref="BitProModal"/> that is shown using the <see cref="BitProModalService"/>.
/// </summary>
public class BitProModalParameters
{
    /// <summary>
    /// When true, the Modal will be positioned absolute instead of fixed.
    /// </summary>
    public bool? AbsolutePosition { get; set; }

    /// <summary>
    /// The aria-label of the Modal for accessibility.
    /// </summary>
    public string? AriaLabel { get; set; }

    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Modal.
    /// </summary>
    public bool? AutoToggleScroll { get; set; }

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).
    /// </summary>
    public bool? Blocking { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitProModal component.
    /// </summary>
    public BitProModalClassStyles? Classes { get; set; }

    /// <summary>
    /// The title (and aria-label) of the close button for accessibility and localization.
    /// </summary>
    public string? CloseButtonTitle { get; set; }

    /// <summary>
    /// Gets or sets the icon to display in the close button using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the close button from the built-in Fluent UI icons.
    /// </summary>
    public string? CloseIconName { get; set; }

    /// <summary>
    /// The general directionality of the Modal.
    /// </summary>
    public BitDir? Dir { get; set; }

    /// <summary>
    /// The CSS selector of the drag element. by default it's the Modal container.
    /// </summary>
    public string? DragElementSelector { get; set; }

    /// <summary>
    /// Whether the Modal can be dragged around.
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
    /// Makes the Modal height 100% of its parent container.
    /// </summary>
    public bool? FullHeight { get; set; }

    /// <summary>
    /// Makes the Modal width and height 100% of its parent container.
    /// </summary>
    public bool? FullSize { get; set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container.
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
    /// Capture and render additional attributes in addition to the component's parameters.
    /// </summary>
    public Dictionary<string, object> HtmlAttributes { get; set; } = [];

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
    /// </summary>
    public bool? IsAlert { get; set; }

    /// <summary>
    /// Whether or not the Modal is enabled.
    /// </summary>
    public bool? IsEnabled { get; set; }

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    public bool? ModeFull { get; set; }

    /// <summary>
    /// Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal).
    /// </summary>
    public bool? Modeless { get; set; }

    /// <summary>
    /// Removes the default top border of the Modal.
    /// </summary>
    public bool? NoBorder { get; set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    public EventCallback<MouseEventArgs> OnDismiss { get; set; }

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
    /// Set the element reference for which the Modal disables its scroll if applicable.
    /// </summary>
    public ElementReference? ScrollerElement { get; set; }

    /// <summary>
    /// Set the element selector for which the Modal disables its scroll if applicable.
    /// </summary>
    public string? ScrollerSelector { get; set; }

    /// <summary>
    /// Shows the close button of the Modal.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitProModal component.
    /// </summary>
    public BitProModalClassStyles? Styles { get; set; }

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
    /// Merges two sets of <see cref="BitProModalParameters"/> giving precedence to the values of the first one.
    /// </summary>
    public static BitProModalParameters? Merge(BitProModalParameters? params1, BitProModalParameters? params2)
    {
        if (params1 is null && params2 is null) return null;

        if (params2 is null) return params1;
        if (params1 is null) return params2;


        return new BitProModalParameters
        {
            AbsolutePosition = params1.AbsolutePosition ?? params2.AbsolutePosition,
            AriaLabel = params1.AriaLabel ?? params2.AriaLabel,
            AutoToggleScroll = params1.AutoToggleScroll ?? params2.AutoToggleScroll,
            Blocking = params1.Blocking ?? params2.Blocking,
            Classes = BitProModalClassStyles.Merge(params1.Classes, params2.Classes),
            CloseButtonTitle = params1.CloseButtonTitle ?? params2.CloseButtonTitle,
            CloseIcon = params1.CloseIcon ?? params2.CloseIcon,
            CloseIconName = params1.CloseIconName ?? params2.CloseIconName,
            Dir = params1.Dir ?? params2.Dir,
            DragElementSelector = params1.DragElementSelector ?? params2.DragElementSelector,
            Draggable = params1.Draggable ?? params2.Draggable,
            Footer = params1.Footer ?? params2.Footer,
            FooterText = params1.FooterText ?? params2.FooterText,
            FullHeight = params1.FullHeight ?? params2.FullHeight,
            FullSize = params1.FullSize ?? params2.FullSize,
            FullWidth = params1.FullWidth ?? params2.FullWidth,
            Header = params1.Header ?? params2.Header,
            HeaderText = params1.HeaderText ?? params2.HeaderText,
            HtmlAttributes = (params2.HtmlAttributes ?? []).Concat(params1.HtmlAttributes ?? []).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.Last().Value),
            IsAlert = params1.IsAlert ?? params2.IsAlert,
            IsEnabled = params1.IsEnabled ?? params2.IsEnabled,
            ModeFull = params1.ModeFull ?? params2.ModeFull,
            Modeless = params1.Modeless ?? params2.Modeless,
            NoBorder = params1.NoBorder ?? params2.NoBorder,
            // Only compose a callback when at least one side actually has a delegate. Otherwise leave
            // the merged callback at its default (HasDelegate == false) to preserve the empty contract,
            // so consumers (e.g. the inner BitModal) don't see a handler that does nothing.
            OnDismiss = (params1.OnDismiss.HasDelegate || params2.OnDismiss.HasDelegate)
                ? EventCallback.Factory.Create<MouseEventArgs>(new object(), async (MouseEventArgs e) =>
                {
                    await params1.OnDismiss.InvokeAsync(e);
                    await params2.OnDismiss.InvokeAsync(e);
                })
                : default,
            OnOpen = (params1.OnOpen.HasDelegate || params2.OnOpen.HasDelegate)
                ? EventCallback.Factory.Create(new object(), async () =>
                {
                    await params1.OnOpen.InvokeAsync();
                    await params2.OnOpen.InvokeAsync();
                })
                : default,
            OnOverlayClick = (params1.OnOverlayClick.HasDelegate || params2.OnOverlayClick.HasDelegate)
                ? EventCallback.Factory.Create<MouseEventArgs>(new object(), async (MouseEventArgs e) =>
                {
                    await params1.OnOverlayClick.InvokeAsync(e);
                    await params2.OnOverlayClick.InvokeAsync(e);
                })
                : default,
            Position = params1.Position ?? params2.Position,
            ScrollerElement = params1.ScrollerElement ?? params2.ScrollerElement,
            ScrollerSelector = params1.ScrollerSelector ?? params2.ScrollerSelector,
            ShowCloseButton = params1.ShowCloseButton ?? params2.ShowCloseButton,
            Styles = BitProModalClassStyles.Merge(params1.Styles, params2.Styles),
            SubtitleAriaId = params1.SubtitleAriaId ?? params2.SubtitleAriaId,
            TitleAriaId = params1.TitleAriaId ?? params2.TitleAriaId,
            Visibility = params1.Visibility ?? params2.Visibility,
        };
    }
}
