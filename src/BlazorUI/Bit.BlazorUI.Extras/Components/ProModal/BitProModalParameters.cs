namespace Bit.BlazorUI;

/// <summary>
/// The set of parameters used to customize a <see cref="BitProModal"/> that is shown using the <see cref="BitProModalService"/>.
/// </summary>
public class BitProModalParameters
{
    /// <summary>
    /// When true, the Modal will be positioned absolute instead of fixed.
    /// </summary>
    public bool AbsolutePosition { get { return _proModal?.AbsolutePosition is true ? true : field; } set; }

    /// <summary>
    /// Enables the auto scrollbar toggle behavior of the Modal.
    /// </summary>
    public bool AutoToggleScroll { get { return _proModal?.AutoToggleScroll is true ? true : field; } set; }

    /// <summary>
    /// When enabled, prevents the Modal from being light dismissed by clicking outside the Modal (on the overlay).
    /// </summary>
    public bool Blocking { get { return _proModal?.Blocking is true ? true : field; } set; }

    /// <summary>
    /// Custom CSS classes for different parts of the BitProModal component.
    /// </summary>
    public BitProModalClassStyles? Classes { get; set; }

    /// <summary>
    /// The title (and aria-label) of the close button for accessibility and localization.
    /// </summary>
    public string CloseButtonTitle { get { return _proModal is not null && _proModal.CloseButtonTitle != "Close" ? _proModal.CloseButtonTitle : field; } set; } = "Close";

    /// <summary>
    /// Gets or sets the icon to display in the close button using custom CSS classes for external icon libraries.
    /// </summary>
    public BitIconInfo? CloseIcon { get { return _proModal?.CloseIcon ?? field; } set; }

    /// <summary>
    /// Gets or sets the name of the icon to display in the close button from the built-in Fluent UI icons.
    /// </summary>
    public string? CloseIconName { get { return _proModal?.CloseIconName ?? field; } set; }

    /// <summary>
    /// The general directionality of the Modal.
    /// </summary>
    public BitDir? Dir { get { return _proModal?.Dir ?? field; } set; }

    /// <summary>
    /// The CSS selector of the drag element. by default it's the Modal container.
    /// </summary>
    public string? DragElementSelector { get { return _proModal?.DragElementSelector ?? field; } set; }

    /// <summary>
    /// Whether the Modal can be dragged around.
    /// </summary>
    public bool Draggable { get { return _proModal?.Draggable is true ? true : field; } set; }

    /// <summary>
    /// The template used to render the footer section of the Modal.
    /// </summary>
    public RenderFragment? Footer { get { return _proModal?.Footer ?? field; } set; }

    /// <summary>
    /// The text of the footer section of the Modal.
    /// </summary>
    public string? FooterText { get { return _proModal?.FooterText ?? field; } set; }

    /// <summary>
    /// Makes the Modal height 100% of its parent container.
    /// </summary>
    public bool FullHeight { get { return _proModal?.FullHeight is true ? true : field; } set; }

    /// <summary>
    /// Makes the Modal width and height 100% of its parent container.
    /// </summary>
    public bool FullSize { get { return _proModal?.FullSize is true ? true : field; } set; }

    /// <summary>
    /// Makes the Modal width 100% of its parent container.
    /// </summary>
    public bool FullWidth { get { return _proModal?.FullWidth is true ? true : field; } set; }

    /// <summary>
    /// The template used to render the header section of the Modal.
    /// </summary>
    public RenderFragment? Header { get { return _proModal?.Header ?? field; } set; }

    /// <summary>
    /// The text of the header section of the Modal.
    /// </summary>
    public string? HeaderText { get { return _proModal?.HeaderText ?? field; } set; }

    /// <summary>
    /// Capture and render additional attributes in addition to the component's parameters.
    /// </summary>
    public Dictionary<string, object> HtmlAttributes { get { return _proModal?.HtmlAttributes ?? field; } set; } = [];

    /// <summary>
    /// Determines the ARIA role of the Modal (alertdialog/dialog).
    /// </summary>
    public bool? IsAlert { get { return _proModal?.IsAlert ?? field; } set; }

    /// <summary>
    /// Whether or not the Modal is enabled.
    /// </summary>
    public bool IsEnabled { get { return _proModal?.IsEnabled is false ? false : field; } set; } = true;

    /// <summary>
    /// Renders the overlay in full mode that gives it an opaque background.
    /// </summary>
    public bool ModeFull { get { return _proModal?.ModeFull is true ? true : field; } set; }

    /// <summary>
    /// Whether the Modal should be modeless (e.g. not dismiss when focusing/clicking outside of the Modal).
    /// </summary>
    public bool Modeless { get { return _proModal?.Modeless is true ? true : field; } set; }

    /// <summary>
    /// Removes the default top border of the Modal.
    /// </summary>
    public bool NoBorder { get { return _proModal?.NoBorder is true ? true : field; } set; }

    /// <summary>
    /// A callback function for when the Modal is dismissed.
    /// </summary>
    public EventCallback<MouseEventArgs> OnDismiss
    {
        get
        {
            return EventCallback.Factory.Create<MouseEventArgs>(new object(), async () =>
            {
                if (_proModal is not null) await _proModal.OnDismiss.InvokeAsync();
                await field.InvokeAsync();
            });
        }
        set;
    }

    /// <summary>
    /// A callback function for when the Modal is opened.
    /// </summary>
    public EventCallback OnOpen
    {
        get
        {
            return EventCallback.Factory.Create(new object(), async () =>
            {
                if (_proModal is not null) await _proModal.OnOpen.InvokeAsync();
                await field.InvokeAsync();
            });
        }
        set;
    }

    /// <summary>
    /// A callback function for when somewhere on the overlay element of the Modal is clicked.
    /// </summary>
    public EventCallback<MouseEventArgs> OnOverlayClick
    {
        get
        {
            return EventCallback.Factory.Create<MouseEventArgs>(new object(), async () =>
            {
                if (_proModal is not null) await _proModal.OnOverlayClick.InvokeAsync();
                await field.InvokeAsync();
            });
        }
        set;
    }

    /// <summary>
    /// Position of the Modal on the screen.
    /// </summary>
    public BitPosition? Position { get { return _proModal?.Position ?? field; } set; }

    /// <summary>
    /// Set the element reference for which the Modal disables its scroll if applicable.
    /// </summary>
    public ElementReference? ScrollerElement { get { return _proModal?.ScrollerElement ?? field; } set; }

    /// <summary>
    /// Set the element selector for which the Modal disables its scroll if applicable.
    /// </summary>
    public string? ScrollerSelector { get { return _proModal?.ScrollerSelector ?? field; } set; }

    /// <summary>
    /// Shows the close button of the Modal.
    /// </summary>
    public bool ShowCloseButton { get { return _proModal?.ShowCloseButton is true ? true : field; } set; }

    /// <summary>
    /// Custom CSS styles for different parts of the BitProModal component.
    /// </summary>
    public BitProModalClassStyles? Styles { get; set; }

    /// <summary>
    /// ARIA id for the subtitle of the Modal, if any.
    /// </summary>
    public string? SubtitleAriaId { get { return _proModal?.SubtitleAriaId ?? field; } set; }

    /// <summary>
    /// ARIA id for the title of the Modal, if any.
    /// </summary>
    public string? TitleAriaId { get { return _proModal?.TitleAriaId ?? field; } set; }


    private BitProModal? _proModal;
    public void SetProModal(BitProModal proModal)
    {
        _proModal = proModal;
    }


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
            AbsolutePosition = params1.AbsolutePosition || params2.AbsolutePosition,
            AutoToggleScroll = params1.AutoToggleScroll || params2.AutoToggleScroll,
            Blocking = params1.Blocking || params2.Blocking,
            Classes = BitProModalClassStyles.Merge(params1.Classes, params2.Classes),
            CloseButtonTitle = params1.CloseButtonTitle != "Close" ? params1.CloseButtonTitle : params2.CloseButtonTitle,
            CloseIcon = params1.CloseIcon ?? params2.CloseIcon,
            CloseIconName = params1.CloseIconName ?? params2.CloseIconName,
            Dir = params1.Dir ?? params2.Dir,
            DragElementSelector = params1.DragElementSelector ?? params2.DragElementSelector,
            Draggable = params1.Draggable || params2.Draggable,
            Footer = params1.Footer ?? params2.Footer,
            FooterText = params1.FooterText ?? params2.FooterText,
            FullHeight = params1.FullHeight || params2.FullHeight,
            FullSize = params1.FullSize || params2.FullSize,
            FullWidth = params1.FullWidth || params2.FullWidth,
            Header = params1.Header ?? params2.Header,
            HeaderText = params1.HeaderText ?? params2.HeaderText,
            HtmlAttributes = params1.HtmlAttributes.Concat(params2.HtmlAttributes).ToDictionary(kv => kv.Key, kv => kv.Value),
            IsAlert = params1.IsAlert ?? params2.IsAlert,
            IsEnabled = params1.IsEnabled && params2.IsEnabled,
            ModeFull = params1.ModeFull || params2.ModeFull,
            Modeless = params1.Modeless || params2.Modeless,
            NoBorder = params1.NoBorder || params2.NoBorder,
            OnDismiss = EventCallback.Factory.Create<MouseEventArgs>(new object(), async () =>
            {
                await params1.OnDismiss.InvokeAsync();
                await params2.OnDismiss.InvokeAsync();
            }),
            OnOpen = EventCallback.Factory.Create(new object(), async () =>
            {
                await params1.OnOpen.InvokeAsync();
                await params2.OnOpen.InvokeAsync();
            }),
            OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(new object(), async () =>
            {
                await params1.OnOverlayClick.InvokeAsync();
                await params2.OnOverlayClick.InvokeAsync();
            }),
            Position = params1.Position ?? params2.Position,
            ScrollerElement = params1.ScrollerElement ?? params2.ScrollerElement,
            ScrollerSelector = params1.ScrollerSelector ?? params2.ScrollerSelector,
            ShowCloseButton = params1.ShowCloseButton || params2.ShowCloseButton,
            Styles = BitProModalClassStyles.Merge(params1.Styles, params2.Styles),
            SubtitleAriaId = params1.SubtitleAriaId ?? params2.SubtitleAriaId,
            TitleAriaId = params1.TitleAriaId ?? params2.TitleAriaId,
        };
    }
}
