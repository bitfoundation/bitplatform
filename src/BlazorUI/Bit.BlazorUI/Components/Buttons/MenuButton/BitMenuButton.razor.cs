using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI;

/// <summary>
/// A menu button combines a button with a callout menu of related actions or links.
/// It supports split and sticky modes, toggle behavior, nested submenus, check and single-choice items,
/// group headers, separators, links, keyboard shortcuts, a loading state, and full keyboard navigation with
/// proper ARIA menu semantics.
/// </summary>
public partial class BitMenuButton<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem> : BitComponentBase where TItem : class
{
    private int _clickId;
    private bool _internalIsOpenChange;
    private bool _repositionAfterRender;
    private List<TItem> _items = [];
    private bool _showLoading;
    internal BitButtonType _buttonType;
    // The list the callout carries, which is what the trigger's aria-controls names: the APG menu button
    // pattern points the button at the menu itself rather than at the element the menu is drawn inside,
    // which is what lets a reader move from the button to the popup it has just announced.
    private string _menuId = default!;
    private string _calloutId = default!;
    private string _overlayId = default!;
    // The zero-sized element a menu opened at a point is placed against (see OpenAt), and the point itself.
    private string _pointId = default!;
    private double? _pointX;
    private double? _pointY;
    private bool _openAtPointAfterRender;
    private bool _moveToPointAfterRender;
    // Whether the element the focus was on when the menu was opened at a point is remembered, for the menu to hand
    // the focus back to it as it closes rather than to a trigger the user never went near.
    private bool _focusOriginStored;
    private bool _autoFocus;
    private bool _focusFirstItemOnOpen;
    private int _hoverToken;
    private bool _hasSubmenus;
    // The OpenOnHover mode (see HoverDriven): whether the pointer of the device can hover at all, whether it is on
    // the menu button or on one of its menus, the open or close it is waiting out, and who the open menu answers to.
    private bool? _isHoverDevice;
    private bool _hoverInside;
    private CancellationTokenSource? _hoverCts;
    private CalloutOwner _owner;
    private bool _hoverKeyboardRegistered;
    private BitNavCurrentUrlMatcher<TItem>? _currentUrlMatcher;
    // Whether any item says how it matches the current URL, read on the first navigation after the items may have
    // changed rather than on every one (see OnLocationChanged); null until then.
    private bool? _hasMatches;
    // A change of HasOverlay while the menu is open, which the JS side has to be told about (see OnAfterRenderAsync).
    private bool _retoggleAfterRender;
    private readonly BitTypeahead _typeahead = new();
    private string _typeaheadCalloutId = string.Empty;
    private ElementReference _operatorButtonRef;
    private ElementReference _chevronButtonRef;
    private CancellationTokenSource? _loadingDelayCts;
    private IEnumerable<TItem> _oldItems = default!;
    // The submenus that are open, outermost first. It is a path rather than a set: opening one closes
    // whatever was open beside it, so at most one submenu is open per level of the menu.
    private readonly List<IBitMenuButtonSubmenu> _openSubmenus = [];
    private DotNetObjectReference<BitMenuButton<TItem>> _dotnetObj = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;

    [Inject] private NavigationManager _navigationManager { get; set; } = default!;



    /// <summary>
    /// The EditContext, which is set if the menu button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] protected EditContext? EditContext { get; set; }

    /// <summary>
    /// Gets or sets the cascading parameters for the menu button component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings to be applied to multiple menu button components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitMenuButtonParams.ParamName)]
    public BitMenuButtonParams? CascadingParameters { get; set; }



    /// <summary>
    /// Detailed description of the menu button for the benefit of screen readers (rendered into
    /// <c>aria-describedby</c>).
    /// </summary>
    /// <remarks>
    /// It is rendered as visually hidden text beside the button and read after its name, not as part of it.
    /// An <c>aria-describedby</c> written on the component by hand is kept and this description is added to it,
    /// since the attribute is a list of ids.
    /// </remarks>
    [Parameter] public string? AriaDescription { get; set; }

    /// <summary>
    /// If true, add an aria-hidden attribute instructing screen readers to ignore the menu button.
    /// </summary>
    [Parameter] public bool AriaHidden { get; set; }

    /// <summary>
    /// If true, the header button automatically receives focus when the page renders
    /// (rendered as the <c>autofocus</c> attribute).
    /// </summary>
    /// <remarks>
    /// It is dropped on a menu button that is hidden from assistive technologies, and on a disabled one that
    /// is not kept focusable, since neither should be able to pull the focus on the first render.
    /// </remarks>
    [Parameter] public bool AutoFocus { get; set; }

    /// <summary>
    /// If true, the menu button enters the loading state automatically while awaiting <see cref="OnClick"/>
    /// and ignores further clicks of the header button until it returns.
    /// </summary>
    /// <remarks>
    /// It is meant for the main half of a split menu button, which is a command of its own; the chevron
    /// keeps opening the menu while the command runs.
    /// </remarks>
    [Parameter] public bool AutoLoading { get; set; }

    /// <summary>
    /// The background color kind of the callout.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over <c>--bit-MenuButton-callout-background</c>; left unset, the callout takes the
    /// primary background unless that variable says otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColorKind? Background { get; set; }

    /// <summary>
    /// The value of the type attribute of the items, and of the main half of a split menu button when it is set.
    /// Left unset, the items are submit buttons inside an <see cref="EditForm"/> and plain buttons elsewhere; a submit
    /// or reset item acts on the form the menu button sits in, although its open menu is moved out of it to the body.
    /// </summary>
    [Parameter] public BitButtonType? ButtonType { get; set; }

    /// <summary>
    /// The icon of the check mark shown on a checked item, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="CheckIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CheckIcon { get; set; }

    /// <summary>
    /// The name of the icon of the check mark shown on a checked item.
    /// </summary>
    [Parameter] public string? CheckIconName { get; set; }

    /// <summary>
    /// The aria-label of the chevron down button of the split menu button for the benefit of screen readers.
    /// Defaults to <c>More options</c>: the chevron carries no text of its own, so without a name it reaches
    /// a screen reader as an unlabelled button.
    /// </summary>
    [Parameter] public string? ChevronDownAriaLabel { get; set; }

    /// <summary>
    /// The icon for the chevron down part of the menu button.
    /// </summary>
    [Parameter] public BitIconInfo? ChevronDownIcon { get; set; }

    /// <summary>
    /// The icon name of the chevron down part of the menu button.
    /// </summary>
    [Parameter] public string? ChevronDownIconName { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the chevron down button of the split menu button.
    /// </summary>
    [Parameter] public string? ChevronDownTitle { get; set; }

    /// <summary>
    /// The content of the menu button, that are BitMenuButtonOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the menu button.
    /// </summary>
    [Parameter, ResetClassBuilder] public BitMenuButtonClassStyles? Classes { get; set; }

    /// <summary>
    /// Closes the callout when an item is clicked, which is what a menu of one-off commands wants.
    /// </summary>
    /// <remarks>
    /// Turn it off for a menu the user works inside of - a set of checkable items such as a column picker -
    /// so that several items can be toggled without reopening the menu between them.
    /// </remarks>
    [Parameter] public bool CloseOnItemClick { get; set; } = true;

    /// <summary>
    /// The general color of the menu button.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over the <c>--bit-MenuButton-*</c> color variables, the toggled ones and the item
    /// focus and check mark colors included; left unset, the menu button is primary unless those variables say
    /// otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// Default value of the SelectedItem.
    /// </summary>
    [Parameter] public TItem? DefaultSelectedItem { get; set; }

    /// <summary>
    /// Default value of the Toggled parameter in toggle mode.
    /// </summary>
    [Parameter] public bool? DefaultToggled { get; set; }

    /// <summary>
    /// Keeps a disabled menu button, and the disabled items of any menu button, focusable: the disabled state is
    /// conveyed with the <c>aria-disabled</c> attribute instead of the native <c>disabled</c> one, so the button
    /// stays in the tab order, the arrow keys still reach the items, and a screen reader announces both that they
    /// exist and that they are unavailable. Their actions stay suppressed either way.
    /// </summary>
    [Parameter] public bool DisabledInteractive { get; set; }

    /// <summary>
    /// Determines the allowed drop directions of the callout.
    /// </summary>
    [Parameter] public BitDropDirection DropDirection { get; set; } = BitDropDirection.TopAndBottom;

    /// <summary>
    /// The id of the form element the menu button is associated with (rendered as the <c>form</c> attribute of
    /// the header button and of the items). It lets a submit or reset command sit outside of its form element.
    /// </summary>
    [Parameter] public string? FormId { get; set; }

    /// <summary>
    /// Expands the menu button width to 100% of the available width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The content inside the header of menu button can be customized. The loading state takes the header
    /// over while it lasts, so a menu button that is loading shows its spinner (or its
    /// <see cref="LoadingTemplate"/>) rather than this template.
    /// </summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    /// <summary>
    /// The delay in milliseconds before the menu closes once the pointer leaves the menu button and its menus in the
    /// <see cref="OpenOnHover"/> mode. It bridges the gap between the button and the menu, and between the menu and a
    /// submenu, so moving the pointer from one to the other does not close what the pointer is on its way to.
    /// Defaults to 150.
    /// </summary>
    [Parameter] public int HoverCloseDelay { get; set; } = 150;

    /// <summary>
    /// The delay in milliseconds before the menu opens once the pointer enters the menu button in the
    /// <see cref="OpenOnHover"/> mode, so that passing over the button on the way somewhere else does not open it.
    /// Defaults to 0, which opens it as soon as the pointer arrives.
    /// </summary>
    [Parameter] public int HoverOpenDelay { get; set; }

    /// <summary>
    /// Gets or sets the icon to display using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="IconName"/> when both are set.
    /// </summary>
    /// <remarks>
    /// Use this property to render icons from external libraries like FontAwesome, or Bootstrap Icons.
    /// For built-in Fluent UI icons, use <see cref="IconName"/> instead.
    /// </remarks>
    [Parameter] public BitIconInfo? Icon { get; set; }

    /// <summary>
    /// The icon to show inside the header of menu button.
    /// </summary>
    [Parameter] public string? IconName { get; set; }

    /// <summary>
    /// Renders the header button as its icon alone: the text and the chevron beside it are dropped and the
    /// button becomes a square of the control's own height. It is the overflow menu of a toolbar or a row -
    /// a glyph that opens a menu and nothing else.
    /// </summary>
    /// <remarks>
    /// The button then has no text to be named by, so give it an <c>AriaLabel</c> (or a
    /// <see cref="Title"/>). In split mode only the main half is squared up: the chevron half is the only
    /// way into the menu, so it stays. A <see cref="HeaderTemplate"/> is the page's own markup and is
    /// rendered as written.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public bool IconOnly { get; set; }

    /// <summary>
    /// Determines the opening state of the callout.
    /// </summary>
    [Parameter]
    [CallOnSet(nameof(OnSetIsOpen))]
    [ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool IsOpen { get; set; }

    /// <summary>
    ///  List of items to show in the menu button.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// The custom template content to render each item.
    /// </summary>
    [Parameter] public RenderFragment<TItem?>? ItemTemplate { get; set; }

    /// <summary>
    /// Determines whether the menu button is in the loading state.
    /// It replaces the default icon of the header button with a spinner and ignores its click.
    /// In split mode the chevron still opens the menu, so the rest of the commands stay reachable.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    public bool Loading { get; set; }

    /// <summary>
    /// The delay in milliseconds before the spinner appears after the menu button enters the loading state,
    /// which is what keeps a fast operation from flashing one. The click guard of the loading state applies
    /// immediately regardless of the delay.
    /// </summary>
    [Parameter] public int LoadingDelay { get; set; }

    /// <summary>
    /// The text to show beside the spinner while the menu button is in the loading state, replacing the text of
    /// the header button. It is also announced by screen readers through a status live region when the loading
    /// state starts, which is what tells a user who cannot see the spinner that the operation is running.
    /// </summary>
    [Parameter] public string? LoadingLabel { get; set; }

    /// <summary>
    /// The custom template that replaces the spinner and the label of the header button while the menu button
    /// is in the loading state. The chevron of a split button is kept beside it.
    /// </summary>
    [Parameter] public RenderFragment? LoadingTemplate { get; set; }

    /// <summary>
    /// The tallest the callout grows before its items start to scroll, as a CSS length (e.g. <c>12rem</c>).
    /// Without one the callout is capped to the room the viewport leaves below or above the button.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitMenuButtonNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Replaces the text the items opening a new tab are announced with, for translating it or for saying it
    /// another way.
    /// </summary>
    /// <remarks>
    /// A link opening a new tab takes the reader somewhere the back button no longer returns from, so one whose
    /// target is <c>_blank</c> carries the sentence saying so - "<c>(opens in a new tab)</c>" unless this
    /// replaces it - as visually hidden text after its content, or appended to its aria-label when it has one,
    /// since an aria-label replaces the content rather than adding to it.
    /// <br />
    /// An empty value takes the announcement off, the same as <see cref="NoNewTabHint"/> does.
    /// </remarks>
    [Parameter] public string? NewTabHint { get; set; }

    /// <summary>
    /// If true, removes the icon from the header button.
    /// </summary>
    [Parameter] public bool NoIcon { get; set; }

    /// <summary>
    /// Stops the items opening a new tab from announcing that they do.
    /// </summary>
    /// <remarks>
    /// Only set this where the announcement would be made twice - on items whose own labels already say they open
    /// a new tab, or under a heading that already says every one of them does. See <see cref="NewTabHint"/> for
    /// what is being taken off.
    /// </remarks>
    [Parameter] public bool NoNewTabHint { get; set; }

    /// <summary>
    /// Leaves the page its own clicks while the menu is open, by not rendering the overlay that otherwise covers
    /// it. Whatever is underneath the menu can be interacted with, and a click or a right-click outside of the
    /// menu and its button dismisses it.
    /// </summary>
    /// <remarks>
    /// This is what a context menu opened with <see cref="OpenAt(MouseEventArgs)"/> needs: the overlay would take
    /// the right-click that is meant to move the menu to a new point, leaving the browser to show its own menu on
    /// top of it instead. Without it, a right-click the page takes for itself (its handler prevents the default)
    /// moves the menu, and any other one dismisses it.
    /// </remarks>
    [Parameter] public bool NoOverlay { get; set; }

    /// <summary>
    /// The callback that is called when the selected item has changed.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnChange { get; set; }

    /// <summary>
    /// The callback that is called when the header button is clicked, and when an item of the menu is activated
    /// outside of <see cref="Sticky"/> mode.
    /// </summary>
    /// <remarks>
    /// It carries the item the click belongs to: the selected one for the header of a sticky menu button,
    /// the activated one for an item, and <c>null</c> for the header of a menu button that is not sticky.
    /// In sticky mode an item's activation is a selection rather than a command, so it raises
    /// <see cref="OnChange"/> instead.
    /// </remarks>
    [Parameter] public EventCallback<TItem?> OnClick { get; set; }

    /// <summary>
    /// The callback that is called when the Toggled value changes in toggle mode.
    /// </summary>
    [Parameter] public EventCallback<bool> OnToggleChange { get; set; }

    /// <summary>
    /// Opens the menu when the pointer enters the menu button and closes it when the pointer leaves both the button
    /// and the menu - and any submenu opened from it - which is what a navigation menu is usually expected to do.
    /// The button keeps toggling the menu on a click, so the keyboard and the touch screens - where hovering does not
    /// exist and this mode turns itself off - are left with a way to reach it.
    /// </summary>
    /// <remarks>
    /// A menu opened by hovering leaves the focus where it is, and Escape closes it wherever the focus is, until an
    /// arrow key, Enter or Space on the button, or the keyboard moving the focus into it, hands it to the keyboard.
    /// A menu the keyboard holds is left open when the pointer leaves it. No overlay is rendered in this mode, as
    /// with <see cref="NoOverlay"/>: it would take the pointer away from the button the menu was opened from, and a
    /// click outside the menu and its button still dismisses it.
    /// </remarks>
    [Parameter] public bool OpenOnHover { get; set; }

    /// <summary>
    /// Alias of the ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// The icon of the bullet shown on a checked single-choice item, using custom CSS classes for external
    /// icon libraries. Takes precedence over <see cref="RadioIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? RadioIcon { get; set; }

    /// <summary>
    /// The name of the icon of the bullet shown on a checked single-choice item
    /// (one whose <see cref="BitMenuButtonItem.RadioGroup"/> is set).
    /// </summary>
    [Parameter] public string? RadioIconName { get; set; }

    /// <summary>
    /// Enables re-clicking the header button while the menu button is in the loading state.
    /// By default its click is ignored while the loading lasts, which is what protects against a double submission.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reclickable { get; set; }

    /// <summary>
    /// Shows the menu as a sheet that slides up from the bottom edge of the screen on small screens (an action
    /// sheet), instead of the callout under the button.
    /// </summary>
    /// <remarks>
    /// The sheet is shown below the small breakpoint (600px wide): it runs the full width of the screen and grows
    /// with its items up to the height of the screen, where the items that do not fit scroll inside it, so
    /// <see cref="MaxHeight"/> does not cap it. It closes the ways the callout does: an item, a tap outside it,
    /// Escape. On wider screens the menu is the callout it always is. A submenu still opens beside the item it
    /// belongs to.
    /// </remarks>
    [Parameter] public bool Responsive { get; set; }

    /// <summary>
    /// Determines the current selected item that acts as the header item.
    /// </summary>
    [Parameter, ResetClassBuilder, TwoWayBound]
    [CallOnSet(nameof(OnSetSelectedItem))]
    public TItem? SelectedItem { get; set; }

    /// <summary>
    /// The size of the menu button.
    /// </summary>
    /// <remarks>
    /// An explicit value wins over the <c>--bit-MenuButton-*</c> size variables (min-height, padding, font size, icon
    /// size, chevron width, and the min-height and padding of the items); left unset, the menu button is medium
    /// unless those variables say otherwise.
    /// </remarks>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// If true, the menu button renders as a split button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Split { get; set; }

    /// <summary>
    /// If true, the selected item is going to change the header item.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder]
    public bool Sticky { get; set; }

    /// <summary>
    /// If true, stops the propagation of the click event of the menu button to the parent elements.
    /// Useful when the menu button is placed inside clickable containers like rows or cards.
    /// </summary>
    [Parameter] public bool StopPropagation { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the menu button.
    /// </summary>
    [Parameter, ResetStyleBuilder] public BitMenuButtonClassStyles? Styles { get; set; }

    /// <summary>
    /// The icon of the chevron a submenu item carries, using custom CSS classes for external icon libraries.
    /// Takes precedence over <see cref="SubmenuIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? SubmenuIcon { get; set; }

    /// <summary>
    /// The name of the icon of the chevron an item that opens a submenu carries. It is mirrored in a
    /// right-to-left menu, so one icon serves both directions.
    /// </summary>
    [Parameter] public string? SubmenuIconName { get; set; }

    /// <summary>
    /// The text to show inside the header of menu button.
    /// </summary>
    [Parameter] public string? Text { get; set; }

    /// <summary>
    /// The tooltip to show when the mouse is placed on the header button.
    /// </summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// If true, enables the toggling behavior on the header button in split mode.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder] 
    public bool Toggle { get; set; }

    /// <summary>
    /// Determines whether the header button is in the checked/toggled state when Toggle is enabled.
    /// </summary>
    [Parameter, ResetClassBuilder, ResetStyleBuilder, TwoWayBound]
    public bool Toggled { get; set; }

    /// <summary>
    /// The visual variant of the menu button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    // The form the header and the items are associated with: FormId, or the form attribute the page wrote by hand,
    // so the two halves of the component can never name different forms.
    internal string? ResolvedFormId => FormId ?? GetSplattedAttribute("form");



    /// <summary>
    /// Gives focus to the header button of the menu button, which is the element that carries the menu button
    /// in the tab order (the chevron of a split button is the one beside it).
    /// </summary>
    public ValueTask FocusAsync() => _operatorButtonRef.FocusAsync();

    /// <summary>
    /// Opens the menu at a point on the screen rather than against the menu button, which is what a context menu
    /// needs: the menu is placed against the point the way it is against the button, so it still flips to the side
    /// with the most room and is kept within the screen, and the focus moves to its first item.
    /// </summary>
    /// <param name="x">The horizontal distance in pixels from the left edge of the visible page.</param>
    /// <param name="y">The vertical distance in pixels from the top edge of the visible page.</param>
    /// <remarks>
    /// Calling it again while the menu is open at a point moves it to the new point instead of reopening it. Closing
    /// the menu hands the focus back to the element that had it when the menu was opened, or to the menu button when
    /// there is no such element left. The menu button takes the placement back the next time the menu is opened by
    /// anything else. Nothing is opened while the menu button is disabled, or before it has rendered for the first time.
    /// </remarks>
    public async Task OpenAt(double x, double y)
    {
        if (Disabled || IsDisposed) return;

        // The positioning code is handed the reference the first render creates.
        if (_dotnetObj is null) return;

        var moving = IsOpen && HasPoint;

        // A menu brought to a new point is a fresh one there, so nothing the user had opened or typed into it
        // where it was comes along.
        if (IsOpen)
        {
            await CloseSubmenusFrom(0);

            ResetTypeahead();
        }

        _pointX = x;
        _pointY = y;

        // The element the menu is placed against is rendered from these coordinates, so both the opening and a
        // move to a new point wait for the render that puts it there.
        if (moving)
        {
            _moveToPointAfterRender = true;
        }
        else
        {
            // A menu opened against the button is laid out again against the point, which is what the toggle
            // does for a callout that is already open.
            if (IsOpen is false && await AssignIsOpenInternal(true) is false)
            {
                ForgetPoint();
                return;
            }

            // The focus is taken into a menu opened at a point, so it is the keyboard's rather than the pointer's:
            // the pointer leaving it in the OpenOnHover mode does not close it.
            await TakeOverFromPointer();
            _owner = CalloutOwner.Keyboard;

            // A hover open or close still being waited out belongs to the pointer, which this menu no longer answers to.
            CancelHover();

            _openAtPointAfterRender = true;
        }

        await InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Opens the menu at the point a mouse event happened, which is what a context menu needs. It is the
    /// <see cref="OpenAt(double, double)"/> above, handed the coordinates of the event.
    /// </summary>
    public Task OpenAt(MouseEventArgs e) => OpenAt(e.ClientX, e.ClientY);



    [JSInvokable("CloseCallout")]
    public async Task CloseCalloutBeforeAnotherCalloutIsOpened()
    {
        // The callout is cleared as the menu goes away, and an outer one cleared with it dismisses this one too:
        // neither is a dismissal to report, nor a reason to close the submenus the disposal takes down anyway.
        if (IsDisposing) return;

        // The menu is being dismissed from outside (a click on the page, another callout taking over), which
        // ends the search that was being typed into it just as closing it from the keyboard does.
        ResetTypeahead();

        if (await AssignIsOpen(false) is false) return;

        await DisposeHoverKeyboard();

        StateHasChanged();
    }

    [JSInvokable("OnEscape")]
    public async Task _OnEscape()
    {
        // Escape pressed anywhere in the page outside the menu button and a menu the pointer opened (WCAG 1.4.13),
        // which the JS side only reports while no callout opened from inside it is open. The focus is wherever the
        // user left it, so it is left there.
        if (IsDisposing || IsOpen is false) return;

        await CloseCallout();

        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable("OnFocusIn")]
    public async Task _OnFocusIn()
    {
        // The JS side reports it once and has already released the watch: the focus went into a menu the pointer
        // opened other than by a press of the pointer, which is the keyboard taking it over.
        if (IsDisposing) return;

        await TakeOverFromPointer();
    }



    internal void RegisterOption(BitMenuButtonOption option)
    {
        var item = (option as TItem)!;

        _items.Add(item);

        _hasSubmenus = _hasSubmenus || GetHasChildren(item);
        _hasMatches = null;

        if (Sticky)
        {
            if (SelectedItemHasBeenSet is false && option.IsSelected)
            {
                _ = AssignSelectedItem(item);
            }

            if (SelectedItem is null)
            {
                _ = AssignSelectedItem(_items.FirstOrDefault(IsSelectable));
            }
        }

        StateHasChanged();
    }

    internal void UnregisterOption(BitMenuButtonOption option)
    {
        // The options unregister while they are being disposed, which also happens when the menu button goes away
        // with them: rescanning for submenus and re-rendering once per option is work for a component that is gone.
        if (IsDisposing) return;

        _items.Remove((option as TItem)!);

        _hasSubmenus = _items.Exists(GetHasChildren);
        _hasMatches = null;

        StateHasChanged();
    }



    protected override string RootElementClass => "bit-mnb";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        // Color and Size publish nothing while they are unset, which is what lets the stylesheet tell a default from a
        // choice: the public --bit-MenuButton-* variables restyle the default and never an explicit value.
        ClassBuilder.Register(() => BitCssClasses.Color(Color, "bit-mnb"));

        ClassBuilder.Register(() => IsOpen ? "bit-mnb-omn" : string.Empty);
        ClassBuilder.Register(() => IsOpen ? Classes?.Opened : string.Empty);

        ClassBuilder.Register(() => BitCssClasses.Size(Size, "bit-mnb"));

        ClassBuilder.Register(() => FullWidth ? "bit-mnb-flw" : string.Empty);

        ClassBuilder.Register(() => IconOnly ? "bit-mnb-ion" : string.Empty);

        ClassBuilder.Register(() => Loading ? "bit-mnb-lod" : string.Empty);

        ClassBuilder.Register(() => Loading && Reclickable ? "bit-mnb-rcl" : string.Empty);

        ClassBuilder.Register(() => Split ? "bit-mnb-spl" : "bit-mnb-nsp");

        ClassBuilder.Register(() => Toggle && Split && Toggled ? $"bit-mnb-tgl {Classes?.Toggled}" : string.Empty);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-mnb-fil",
            BitVariant.Outline => "bit-mnb-otl",
            BitVariant.Text => "bit-mnb-txt",
            _ => "bit-mnb-fil"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => IsOpen ? Styles?.Opened : string.Empty);

        StyleBuilder.Register(() => Toggle && Split && Toggled ? Styles?.Toggled : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        _menuId = $"BitMenuButton-{UniqueId}-menu";
        _calloutId = $"BitMenuButton-{UniqueId}-callout";
        _overlayId = $"BitMenuButton-{UniqueId}-overlay";
        _pointId = $"BitMenuButton-{UniqueId}-point";

        _navigationManager.LocationChanged += OnLocationChanged;

        // The cascade is read here as well as in OnParametersSet because DefaultToggled (and the Split and Toggle
        // it depends on) is acted on while initializing, which is before OnParametersSet has run for the first time -
        // a cascaded one applied only there would come too late to seed anything.
        CascadingParameters?.UpdateParameters(this);

        if (Split && Toggle && ToggledHasBeenSet is false && DefaultToggled.HasValue)
        {
            await AssignToggled(DefaultToggled.Value);
        }

        if (Sticky && SelectedItemHasBeenSet is false && DefaultSelectedItem is not null)
        {
            await AssignSelectedItem(DefaultSelectedItem);
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        CascadingParameters?.UpdateParameters(this);

        await base.OnParametersSetAsync();

        _buttonType = ButtonType ?? (EditContext is null ? BitButtonType.Button : BitButtonType.Submit);

        UpdateLoadingVisuals();

        // Options render their items themselves and Blazor skips re-rendering them when only the
        // menu button's own parameters (Styles, Sticky, ItemTemplate, ...) change, so push a re-render to each one.
        RefreshOptions();

        // An item may have been given a Match, or lost one, with any new set of parameters.
        _hasMatches = null;

        // Only the two APIs that register their own items are left alone here - except for whether any of
        // them carries a submenu, which an option is free to start doing at any render (its children are a
        // fragment rather than a registration) and which is cheap to read back off the options themselves.
        if (ChildContent is not null || Options is not null)
        {
            _hasSubmenus = _items.Exists(GetHasChildren);
            return;
        }

        // An empty Items is a real state rather than nothing to do: a menu whose items are taken away has to
        // lose them, not carry on rendering the ones it was given last. The count is compared beside the
        // reference for the same reason in reverse - a list added to in place is the same instance as the one
        // already rendered, and would otherwise never be read again.
        if (Items == _oldItems && _items.Count == Items.Count()) return;

        _oldItems = Items;
        _items = [.. Items];

        _hasSubmenus = _items.Exists(GetHasChildren);

        if (Sticky is false) return;

        if (SelectedItem is not null) return;

        var item = _items.LastOrDefault(GetIsSelected);

        if (item is not null)
        {
            if (await AssignSelectedItem(item) is false) return;
        }
        else
        {
            item = _items.FirstOrDefault(IsSelectable);
            await AssignSelectedItem(item);
        }
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _dotnetObj = DotNetObjectReference.Create(this);
        }

        base.OnAfterRender(firstRender);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            try
            {
                // Prevents the default behavior (scrolling) of the navigation keys handled by the
                // keydown handlers, since Blazor cannot conditionally preventDefault per key.
                await _js.BitMenuButtonsSetup(_Id, _calloutId);
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here

            // The autofocus attribute is written on the header button, but a browser honors it only while the
            // document is still loading: an interactive render replaces the prerendered markup that took the focus,
            // and a WebAssembly page or a menu button rendered later never gets that far. So the first render gives
            // the focus itself, by the very rule the attribute is written by.
            if (_autoFocus && IsDisposed is false)
            {
                await _operatorButtonRef.FocusSafelyAsync();
            }
        }

        // Whether the pointer of the device can hover at all decides whether the hover mode applies and whether the
        // overlay takes the outside clicks, so it is resolved before the menu button is interacted with rather than
        // on the first hover, and only for the menu buttons that ask for it - after the setup of the first render,
        // which nothing here may hold up or skip.
        if (OpenOnHover && _isHoverDevice is null && IsDisposed is false)
        {
            var hadOverlay = HasOverlay;

            _isHoverDevice = await GetIsHoverDevice();

            // A menu already open (IsOpen given from the start, an open right after the render) was handed to the JS
            // side with the overlay it no longer has, which would leave it waiting for a click on an overlay that is
            // hidden from now on: the open callout is registered again, once the render has hidden it.
            _retoggleAfterRender = IsOpen && hadOverlay != HasOverlay;

            StateHasChanged();
        }
        else if (_retoggleAfterRender)
        {
            _retoggleAfterRender = false;

            if (IsOpen && IsDisposed is false)
            {
                await ToggleCallout();
            }
        }

        // A menu opened at a point waits for the render that puts the point on the page. It may have been closed
        // again before that render, in which case there is nothing left to open or move.
        if (_openAtPointAfterRender)
        {
            _openAtPointAfterRender = false;

            if (IsOpen && HasPoint && IsDisposed is false)
            {
                await ShowAtPoint();
            }
        }
        else if (_moveToPointAfterRender)
        {
            _moveToPointAfterRender = false;

            if (IsOpen && HasPoint && IsDisposed is false)
            {
                await MoveToPoint();
            }
        }

        // The menu is placed against the root as the toggle finds it, which can be before the render that shows the
        // root open: a Styles.Opened or Classes.Opened that moves or resizes the root (a margin, a border) lands
        // after the placement - in WebAssembly always, the toggle running in the click's own task. Left there, the
        // menu points at where the root was, and the scroll the browser makes to keep the page steady under the
        // shift reads as the page moving away from the menu, which dismisses it. So it is placed again once the
        // open root is on the page.
        if (_repositionAfterRender)
        {
            _repositionAfterRender = false;

            if (IsOpen && IsDisposed is false)
            {
                try
                {
                    await _js.BitCalloutReposition();
                }
                catch (JSDisconnectedException) { } // we can ignore this exception here
            }
        }
    }



    // Whether any item of the menu is checkable, which is what decides that every item reserves the check
    // column: the labels of the checked and the unchecked rows have to line up with each other, and a column
    // that appears only on the checked ones would shift the label of a row as its state changes. It is read
    // once per item render rather than cached, since an option is free to turn Checkable on at any time.
    internal bool ShowCheckColumn => _items.Exists(GetHasCheckMark);

    // Whether the item carries a state mark at all, which is what reserves the check column: an on/off item
    // shows a check mark, one of a set of mutually exclusive choices a bullet, and both need the same room.
    internal bool GetHasCheckMark(TItem? item) => GetCheckable(item) || GetRadioGroup(item).HasValue();

    // The items a sticky menu button can promote to its header: the ones that are commands in their own right,
    // which leaves out the separators and the group labels that only structure the list around them.
    private bool IsSelectable(TItem item)
    {
        return GetIsDisabled(item) is false
            && GetIsSeparator(item) is false
            && GetIsHeader(item) is false
            // An item that opens a submenu is a way further in rather than a command, so there is nothing
            // for the header of a sticky menu button to carry out once it has been promoted to it.
            && GetHasChildren(item) is false;
    }

    // The glyph of the state mark, which says which kind of state it is: a check mark for an on/off item
    // and a bullet for one of a set of mutually exclusive choices.
    internal string? GetCheckIconCss(TItem? item)
    {
        return GetRadioGroup(item).HasValue()
                ? BitIconInfo.From(RadioIcon, RadioIconName ?? "StatusCircleInner")?.GetCssClasses()
                : BitIconInfo.From(CheckIcon, CheckIconName ?? "Accept")?.GetCssClasses();
    }

    internal string? GetSubmenuChevronCss()
    {
        return BitIconInfo.From(SubmenuIcon, SubmenuIconName ?? "ChevronRight")?.GetCssClasses();
    }

    // Whether anything in the menu opens a submenu, which is what decides that the rows listen to the
    // pointer at all: a menu without one would be paying for a round trip per row hovered and have
    // nothing to do with it. It is answered off a field rather than worked out per row: with a custom
    // item type the answer is read by reflection, and a menu would otherwise read every item once per
    // item it renders.
    internal bool HasSubmenus => _hasSubmenus;

    // The open submenus are a path down the menu, so opening one closes whatever was open at its level
    // and everything that had been opened from inside that.
    internal async Task RegisterOpenSubmenu(IBitMenuButtonSubmenu owner)
    {
        await CloseSubmenusFrom(owner.Level);

        _openSubmenus.Add(owner);
    }

    internal void SubmenuClosed(IBitMenuButtonSubmenu owner)
    {
        _openSubmenus.Remove(owner);
    }

    internal async Task CloseSubmenusFrom(int level)
    {
        // Innermost first, and re-checking the length as it goes: closing a submenu takes it off the list.
        for (var i = _openSubmenus.Count - 1; i >= 0; i--)
        {
            if (i >= _openSubmenus.Count) continue;

            var owner = _openSubmenus[i];

            if (owner.Level < level) continue;

            await owner.CloseSubmenuAsync();
        }
    }

    // A ticket the pointer takes on every row it arrives at, so that the one thing that was going to
    // happen after the hover delay - a submenu opening, or the one already open being taken away - is
    // dropped as soon as the pointer moves on. Arriving in an open submenu takes one too, which is what
    // lets the pointer travel diagonally across the rows between a parent item and the submenu it opened.
    internal int NextHoverToken() => ++_hoverToken;

    internal bool IsHoverTokenCurrent(int token) => _hoverToken == token;

    internal string? GetAriaLabel(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.AriaLabel;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    internal bool GetCheckable(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Checkable;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Checkable;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.Checkable.Selector is not null)
        {
            return NameSelectors.Checkable.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.Checkable.Name, false);
    }

    internal bool GetIsChecked(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsChecked;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsChecked;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsChecked.Selector is not null)
        {
            return NameSelectors.IsChecked.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsChecked.Name, false);
    }

    internal bool GetIsHeader(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsHeader;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsHeader;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsHeader.Selector is not null)
        {
            return NameSelectors.IsHeader.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsHeader.Name, false);
    }

    internal string? GetRadioGroup(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.RadioGroup;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.RadioGroup;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.RadioGroup.Selector is not null)
        {
            return NameSelectors.RadioGroup.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.RadioGroup.Name);
    }

    internal BitLinkRels? GetRel(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Rel;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Rel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Rel.Selector is not null)
        {
            return NameSelectors.Rel.Selector!(item);
        }

        return item.GetValueFromProperty<BitLinkRels?>(NameSelectors.Rel.Name);
    }

    internal string? GetSecondaryText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.SecondaryText;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.SecondaryText;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.SecondaryText.Selector is not null)
        {
            return NameSelectors.SecondaryText.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.SecondaryText.Name);
    }

    internal string? GetDescription(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Description;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Description;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Description.Selector is not null)
        {
            return NameSelectors.Description.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Description.Name);
    }

    private BitNavMatch? GetMatch(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Match;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Match;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Match.Selector is not null)
        {
            return NameSelectors.Match.Selector!(item);
        }

        // The name is read by default, so a custom item class may well have a property of that name that means
        // something else (a bool, a pattern string). Such a property says nothing about matching the current URL,
        // and must not throw from the middle of a render.
        try
        {
            return item.GetValueFromProperty<BitNavMatch?>(NameSelectors.Match.Name);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or ArgumentException)
        {
            return null;
        }
    }

    // Whether the item points at the page the app currently sits on: its Href matched against the current URL the
    // way its Match asks for, with the one rule the navigation components pick their selected item with (see
    // BitNavCurrentUrlMatcher). An item that says nothing about matching is never the current one, so a menu of
    // commands that happen to be links is left as it always was.
    internal bool IsCurrent(TItem? item)
    {
        if (item is null || GetMatch(item) is null) return false;

        _currentUrlMatcher ??= new(_navigationManager, GetHref, static _ => null, i => GetMatch(i) ?? BitNavMatch.Exact);

        return _currentUrlMatcher.IsCurrent(item);
    }

    // Whether the current item sits anywhere in the submenu the item opens, which is what marks the row of a submenu
    // as the way to the current page once the menu is open.
    internal bool HasCurrentChild(TItem? item)
    {
        if (item is null) return false;

        var children = item is BitMenuButtonOption option
                        ? option.ChildItems as List<TItem> ?? []
                        : GetChildItems(item);

        return EnumerateItems(children).Any(IsCurrent);
    }

    internal List<TItem> GetChildItems(TItem? item)
    {
        if (item is null) return [];

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.ChildItems as List<TItem> ?? [];
        }

        // The nested options render themselves inside the submenu their parent draws for them, so there is
        // no list of them to walk here: the fragment is what says the submenu exists.
        if (item is BitMenuButtonOption) return [];

        if (NameSelectors is null) return [];

        if (NameSelectors.ChildItems.Selector is not null)
        {
            return NameSelectors.ChildItems.Selector!(item) ?? [];
        }

        return item.GetValueFromProperty<List<TItem>>(NameSelectors.ChildItems.Name, [])!;
    }

    internal bool GetHasChildren(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.ChildContent is not null;
        }

        return GetChildItems(item).Count > 0;
    }

    internal string? GetClass(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Class;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    internal BitIconInfo? GetIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return BitIconInfo.From(menuButtonItem.Icon, menuButtonItem.IconName);
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return BitIconInfo.From(menuButtonOption.Icon, menuButtonOption.IconName);
        }

        if (NameSelectors is null) return null;

        BitIconInfo? icon = null;
        if (NameSelectors.Icon.Selector is not null)
        {
            icon = NameSelectors.Icon.Selector!(item);
        }
        else
        {
            icon = item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
        }

        string? iconName = null;
        if (NameSelectors.IconName.Selector is not null)
        {
            iconName = NameSelectors.IconName.Selector!(item);
        }
        else
        {
            iconName = item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
        }

        return BitIconInfo.From(icon, iconName);
    }

    internal string? GetHref(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Href;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Href;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Href.Selector is not null)
        {
            return NameSelectors.Href.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Href.Name);
    }

    internal bool GetIsDisabled(TItem? item)
    {
        if (item is null) return true;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsDisabled;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsDisabled;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsDisabled.Selector is not null)
        {
            return NameSelectors.IsDisabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsDisabled.Name, false);
    }

    private bool GetIsSelected(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsSelected;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsSelected;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSelected.Selector is not null)
        {
            return NameSelectors.IsSelected.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsSelected.Name, false);
    }

    internal bool GetIsSeparator(TItem? item)
    {
        if (item is null) return false;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.IsSeparator;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.IsSeparator;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsSeparator.Selector is not null)
        {
            return NameSelectors.IsSeparator.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsSeparator.Name, false);
    }

    private string? GetKey(TItem item)
    {
        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Key;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    internal string? GetStyle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.Style;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    internal string? GetTarget(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Target;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Target;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Target.Selector is not null)
        {
            return NameSelectors.Target.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Target.Name);
    }

    internal RenderFragment<TItem>? GetTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.Template as RenderFragment<TItem>;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    internal string? GetText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem bitMenuButtonItem)
        {
            return bitMenuButtonItem.Text;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    internal string? GetTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitMenuButtonItem menuButtonItem)
        {
            return menuButtonItem.Title;
        }

        if (item is BitMenuButtonOption menuButtonOption)
        {
            return menuButtonOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    private async Task HandleOnHeaderClick(TItem? item)
    {
        if (Disabled) return;
        if (Loading && Reclickable is false) return;

        if (Split is false)
        {
            // The trigger of a plain menu button toggles its menu. With the pointer the overlay takes the
            // click that would close it, but from the keyboard the trigger keeps the focus after it was
            // opened, so Enter and Space arrive here with the menu already open - and have to close it. So
            // does a click on a menu button that renders no overlay (NoOverlay).
            if (IsOpen)
            {
                await HandleOnOpenerClickWhileOpen();
                return;
            }

            await OpenCallout();
        }
        else if (IsOpen)
        {
            // The main half of a split button is a command of its own, and running it is leaving the menu. The
            // overlay takes that click while the menu is open, so it is a menu button without one (NoOverlay) whose
            // main half can be clicked with its menu still open - and the menu has to go with the click.
            await CloseCallout();
            StateHasChanged();
        }

        if (Toggle && Split)
        {
            if (await AssignToggled(!Toggled) is false) return;

            await OnToggleChange.InvokeAsync(Toggled);
        }

        if (item is not null && GetIsDisabled(item)) return;

        var isLoading = Loading;

        // The loading state belongs to the latest click alone (as BitButton's does). A Reclickable menu button's
        // re-click arrives while that state is already on, so it only runs the command again: entering it again
        // would be a no-op, and leaving it when this run ends would clear the spinner of the run that is still in
        // flight - or, for a handler that abandons its previous run by throwing into it, clear it on the way out
        // of the run the user has just replaced.
        var clickId = ++_clickId;

        if (AutoLoading && isLoading is false)
        {
            if (await AssignLoading(true) is false) return;

            UpdateLoadingVisuals();
        }

        try
        {
            await OnClick.InvokeAsync(item);

            // Only the main half of a split button is a command of its own. The header of a plain menu button
            // opens the menu - running the selected item's own action as well would carry out a command the
            // user only asked to choose between, so the sticky header stays a label there.
            if (item is not null && Split)
            {
                await InvokeItemClick(item);
            }
        }
        finally
        {
            if (AutoLoading && clickId == _clickId)
            {
                await AssignLoading(false);

                UpdateLoadingVisuals();
            }
        }
    }

    // The chevron of a split button is the trigger of its menu, so it toggles it the way the header of a plain
    // one does. With the pointer the overlay takes the click that would close an open menu, but from the
    // keyboard the chevron can still be the focused element with the menu open, and Enter has to close it - as
    // does a click on it when there is no overlay (NoOverlay).
    private async Task HandleOnChevronClick()
    {
        if (Disabled) return;

        if (IsOpen)
        {
            await HandleOnOpenerClickWhileOpen();
            return;
        }

        await OpenCallout();
    }

    // A click on the trigger of an open menu closes it - except in two cases of the OpenOnHover mode. A key that
    // activates the trigger of a menu the pointer opened is the keyboard asking for that menu, so it is handed over
    // and the focus taken into it rather than closing what is already showing. And a menu the pointer opened and still
    // holds open from the menu button: closing it there would take away what the user has only just been shown, and
    // moving the pointer off closes it anyway. A menu the keyboard or a click opened is closed by a click as always.
    private async Task HandleOnOpenerClickWhileOpen()
    {
        var byKeyboard = _focusFirstItemOnOpen;
        _focusFirstItemOnOpen = false;

        if (byKeyboard && _owner is CalloutOwner.Pointer)
        {
            await TakeOverFromPointer();
            await FocusItem("first");
            return;
        }

        if (HoverDriven && _hoverInside && _owner is CalloutOwner.Pointer) return;

        await CloseCallout();
        StateHasChanged();
    }

    // The overlay takes the click that dismisses the menu, so the click never reaches a control of its own:
    // whatever had the focus when the menu closed is either the trigger itself, or an item that is now
    // hidden - and a hidden element losing the focus drops it on the document, taking the keyboard back to
    // the top of the page (WCAG 2.4.3). So it is handed back the way Escape does (see ReturnFocus).
    private async Task HandleOnOverlayClick()
    {
        await CloseCallout();

        StateHasChanged();

        await ReturnFocus();
    }

    internal async Task HandleOnItemClick(TItem item)
    {
        if (Disabled || GetIsDisabled(item)) return;

        // A check or radio item's state changes on activation, so it is written before anything is told
        // about the click: a handler that reads the item back sees the state its own click produced.
        var radioGroup = GetRadioGroup(item);

        if (radioGroup.HasValue())
        {
            // Picking one of a set of mutually exclusive choices is picking it, not toggling it: activating
            // the one already chosen leaves it chosen rather than leaving the group with nothing chosen.
            foreach (var other in EnumerateItems())
            {
                if (ReferenceEquals(other, item)) continue;
                if (GetRadioGroup(other) != radioGroup) continue;
                if (GetIsChecked(other) is false) continue;

                await SetIsChecked(other, false);
            }

            await SetIsChecked(item, true);
        }
        else if (GetCheckable(item))
        {
            await SetIsChecked(item, GetIsChecked(item) is false);
        }

        if (CloseOnItemClick)
        {
            await CloseCallout();
        }

        // CloseCallout changes IsOpen but does not re-render the root itself, so refresh now to update
        // the open-state classes even when the Sticky branch below returns early.
        StateHasChanged();

        if (Sticky)
        {
            // A one-way SelectedItem refuses the assignment, which only means the selection did not change -
            // the callout has still been closed here, so the rest of the handler (the focus return below
            // above all) runs either way.
            if (await AssignSelectedItem(item))
            {
                await OnChange.InvokeAsync(item);
            }
        }
        else
        {
            await OnClick.InvokeAsync(item);

            await InvokeItemClick(item);
        }

        // The click handler runs on the clicked item's renderer, so the root element (open state
        // classes) needs an explicit re-render here.
        StateHasChanged();

        // The focused item is hidden along with the callout, so the focus is handed back (see ReturnFocus). A menu
        // that stays open keeps the focus where the user left it, which is what lets the next arrow key
        // continue from the item just toggled instead of starting over.
        if (CloseOnItemClick)
        {
            await ReturnFocus();
        }
    }

    // Every item the menu button renders, however deep: a radio group is identified by its name alone, so
    // the items it clears are looked for across the whole menu rather than only beside the one activated.
    private IEnumerable<TItem> EnumerateItems(List<TItem>? items = null)
    {
        foreach (var item in items ?? _items)
        {
            yield return item;

            var children = item is BitMenuButtonOption option
                            ? option.ChildItems as List<TItem> ?? []
                            : GetChildItems(item);

            foreach (var child in EnumerateItems(children))
            {
                yield return child;
            }
        }
    }

    private async Task SetIsChecked(TItem item, bool value)
    {
        if (item is BitMenuButtonItem menuButtonItem)
        {
            menuButtonItem.IsChecked = value;
        }
        else if (item is BitMenuButtonOption menuButtonOption)
        {
            // An option writes its own parameter back so that @bind-IsChecked reports the change, which a
            // reflection write over the top of it would undo.
            await menuButtonOption.SetIsChecked(value);
        }
        else if (NameSelectors is not null)
        {
            item.SetValueToProperty(NameSelectors.IsChecked.Name, value);
        }
    }

    private async Task InvokeItemClick(TItem item)
    {
        if (item is BitMenuButtonItem menuButtonItem)
        {
            menuButtonItem.OnClick?.Invoke(menuButtonItem);
        }
        else if (item is BitMenuButtonOption menuButtonOption)
        {
            await menuButtonOption.OnClick.InvokeAsync(menuButtonOption);
        }
        else
        {
            if (NameSelectors is null) return;

            if (NameSelectors.OnClick.Selector is not null)
            {
                NameSelectors.OnClick.Selector!(item)?.Invoke(item);
            }
            else
            {
                item.GetValueFromProperty<Action<TItem>?>(NameSelectors.OnClick.Name)?.Invoke(item);
            }
        }
    }

    private async Task HandleOnTriggerKeyDown(KeyboardEventArgs e, bool opener)
    {
        if (Disabled) return;

        if (e.Key is "Escape")
        {
            if (IsOpen is false) return;

            await CloseCallout();
            StateHasChanged();
            return;
        }

        if (opener is false) return;

        // The header of a plain menu button is the only way into the menu, and the loading state is what takes
        // its click away - so it takes the keys that stand in for that click with it, rather than leaving the
        // keyboard a way past a guard the pointer answers to.
        if (Split is false && Loading) return;

        if (e.Key is "ArrowDown" or "ArrowUp")
        {
            // A menu the pointer opened is already showing, so the key is the keyboard asking for it: it is handed
            // over rather than opened again.
            if (IsOpen && _owner is CalloutOwner.Pointer)
            {
                await TakeOverFromPointer();
            }
            else if (await OpenCallout(CalloutOwner.Keyboard) is false) return;

            await FocusItem(e.Key is "ArrowDown" ? "first" : "last");
        }
        else if (e.Key is "Enter" or " ")
        {
            // The native click that follows this keydown opens the callout, so
            // mark it to move the focus to the first item as the APG pattern requires.
            _focusFirstItemOnOpen = true;
        }
    }

    // The key that walks back out of a submenu is the one pointing at the menu it was opened from, which is the
    // opposite of the one that walked into it (_BitMenuButtonItem.HandleOnItemKeyDown), in the direction the button is laid
    // out in - a page's or a content's included, which the menus carry over to the body. Only a horizontal arrow
    // asks, since a direction Dir does not name costs a round trip to read.
    private async ValueTask<bool> IsBackKey(KeyboardEventArgs e)
    {
        if (e.Key is not ("ArrowLeft" or "ArrowRight")) return false;

        return (e.Key is "ArrowRight") == await IsRtlArrowAsync(_js, e.Key);
    }

    // The one keyboard handler of every menu the button renders: the menu itself, and each submenu, which
    // is a callout of its own rather than a part of the menu it was opened from - moved to the body while
    // it is open, so its keys never reach the handler of the menu around it.
    internal async Task HandleOnMenuKeyDown(KeyboardEventArgs e, string calloutId, IBitMenuButtonSubmenu? owner)
    {
        if (Disabled || IsOpen is false) return;

        // A key pressed inside a menu the pointer opened is the keyboard at work in it, so the pointer leaving it no
        // longer closes it from under the focus.
        await TakeOverFromPointer();

        // The level of the items of THIS menu: the submenu of an item at level N holds items at N + 1.
        var level = owner is null ? 0 : owner.Level + 1;

        switch (e.Key)
        {
            case "ArrowDown":
                await CloseSubmenusFrom(level);
                await FocusItemOf(calloutId, "next");
                break;
            case "ArrowUp":
                await CloseSubmenusFrom(level);
                await FocusItemOf(calloutId, "prev");
                break;
            case "Home":
                await CloseSubmenusFrom(level);
                await FocusItemOf(calloutId, "first");
                break;
            case "End":
                await CloseSubmenusFrom(level);
                await FocusItemOf(calloutId, "last");
                break;
            case "Escape":
                // Escape inside a submenu closes that submenu alone and hands the focus back to the item it
                // was opened from, so the menu is walked back out of one level at a time (the APG pattern).
                if (owner is null)
                {
                    await CloseCallout();
                    StateHasChanged();
                    await ReturnFocus();
                }
                else
                {
                    await CloseSubmenusFrom(owner.Level);
                    await owner.FocusAsync();
                }
                break;
            case "Tab":
                // Tab leaves the menu altogether, however deep in it the focus was.
                await CloseCallout();
                StateHasChanged();
                await ReturnFocus();
                break;
            default:
                if (owner is not null && await IsBackKey(e))
                {
                    await CloseSubmenusFrom(owner.Level);
                    await owner.FocusAsync();
                }
                else if (e.Key?.Length is 1 && e.Key != " " && e.CtrlKey is false && e.AltKey is false && e.MetaKey is false)
                {
                    await CloseSubmenusFrom(level);
                    await Typeahead(calloutId, e.Key);
                }
                break;
        }
    }

    // The APG typeahead: the keystrokes that arrive in quick succession are one search string, so a menu with
    // several items under the same letter is reachable by typing further into the label instead of only by
    // cycling through them. A single character repeated, in either case, stays the cycle it has always been.
    // The string is built here, with the rules every typeahead of the library shares (BitTypeahead), and matched
    // against the rendered labels by the script, with the collation of the same culture.
    private async Task Typeahead(string calloutId, string character)
    {
        // A search typed into one menu has nothing to do with the one beside it, so walking into or out of
        // a submenu starts the string over rather than carrying it across.
        if (calloutId != _typeaheadCalloutId)
        {
            _typeahead.Reset();
            _typeaheadCalloutId = calloutId;
        }

        var search = _typeahead.Type(character);

        // A repeated character searches from the item AFTER the focused one, so each press lands on the next
        // match; a real search string searches from the focused one, so the item it already matches is kept.
        await FocusItemOf(calloutId, "char", search.Text, fromCurrent: search.FromNext is false, culture: CultureInfo.CurrentCulture.Name);
    }

    private ValueTask FocusItem(string mode, string? character = null, bool fromCurrent = false)
    {
        return FocusItemOf(_calloutId, mode, character, fromCurrent);
    }

    internal ValueTask FocusItemOf(string calloutId, string mode, string? character = null, bool fromCurrent = false, string? culture = null)
    {
        // A disabled item is normally stepped over, but DisabledInteractive is the page asking for it to be
        // reachable - which is the whole point of keeping it focusable rather than natively disabled.
        return _js.BitMenuButtonsFocusItem(calloutId, mode, character, DisabledInteractive, fromCurrent, culture);
    }

    // Where the focus goes as the menu closes from inside: back to the element it was on when the menu was opened at
    // a point, which is where the user was working, and otherwise - or once that element has left the page - to the
    // trigger the menu belongs to.
    private async Task ReturnFocus()
    {
        if (_focusOriginStored && IsDisposed is false)
        {
            _focusOriginStored = false;

            try
            {
                if (await _js.BitUtilsTryRestoreFocus(_calloutId, onlyWhenLost: true, scopeId: _calloutId)) return;
            }
            catch (JSDisconnectedException) { return; } // we can ignore this exception here
        }

        await FocusTrigger();
    }

    private async Task ShowAtPoint()
    {
        try
        {
            // Remembered before the focus moves into the menu. A focus that is already in it - a menu opened against
            // the button and then moved to a point from its own keyboard - is not an origin, and leaves nothing to
            // hand back but the trigger.
            _focusOriginStored = await _js.BitUtilsTryStoreFocus(_calloutId, scopeId: _calloutId);

            await ToggleCallout();

            // The trigger is not where the user is, so the keyboard has nothing to open the menu from: it is taken
            // straight into the menu, as the arrow keys open it from the trigger.
            await FocusItem("first");
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The menu is already open at a point and was only moved to a new one, so it is laid out again rather than
    // toggled again: going back through the toggle would replay the entry animation of a menu that never went
    // anywhere, and a second right-click somewhere else would flash the menu it only moved.
    private async Task MoveToPoint()
    {
        try
        {
            await _js.BitCalloutReposition();

            await FocusItem("first");
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private bool HasPoint => _pointX.HasValue && _pointY.HasValue;

    private void ForgetPoint()
    {
        _pointX = null;
        _pointY = null;
    }

    // The coordinates are written with the invariant culture, since a comma decimal separator would leave the
    // browser with a length it cannot read.
    private string GetPointStyle()
    {
        var x = _pointX!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var y = _pointY!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return $"left:{x}px;top:{y}px";
    }

    private async Task FocusTrigger()
    {
        if (Split)
        {
            await _chevronButtonRef.FocusSafelyAsync();
        }
        else
        {
            await _operatorButtonRef.FocusSafelyAsync();
        }
    }

    /// <summary>
    /// Opens the callout and reports whether it actually opened, so callers can skip
    /// follow-up work (like focusing an item) on a menu that stayed closed.
    /// </summary>
    private async Task<bool> OpenCallout(CalloutOwner? owner = null)
    {
        // The loading state belongs to the header button alone: in split mode the chevron still opens the menu,
        // so the operation running under the main half never takes the rest of the commands away with it.
        if (Disabled) return false;

        // A menu opened any other way than by the pointer is not the pointer's: an open it is still waiting out would
        // take it over (or open it a second time), and a close it scheduled would shut it from under the keyboard.
        if (owner is not CalloutOwner.Pointer)
        {
            CancelHover();
        }

        // The pointer only passing over the button must not pull the focus out of whatever the user is typing into
        // (WCAG 3.2.1), so a menu it opens leaves the focus where it is.
        var focusFirstItem = _focusFirstItemOnOpen && owner is not CalloutOwner.Pointer;
        _focusFirstItemOnOpen = false;

        if (await AssignIsOpenInternal(true) is false) return false;

        _owner = owner ?? (focusFirstItem ? CalloutOwner.Keyboard : CalloutOwner.Default);

        await ToggleCallout();

        if (_owner is CalloutOwner.Pointer)
        {
            await SetupHoverKeyboard();
        }

        if (focusFirstItem)
        {
            await FocusItem("first");
        }

        return true;
    }

    private async Task CloseCallout()
    {
        // The submenus go first and from the inside out, so that each one is put back where it was
        // rendered while the menu around it is still in the body - which is where it was moved from.
        await CloseSubmenusFrom(0);

        ResetTypeahead();

        if (await AssignIsOpenInternal(false) is false) return;

        await ToggleCallout();
    }

    // The open and close flows follow AssignIsOpen with a ToggleCallout of their own, which they await to order
    // their focus work after it, so the change is marked as theirs and OnSetIsOpen does not toggle the callout a
    // second time (see BitDropdown's twin). Toggled twice, the callout is placed twice in a row, and a click
    // handled in the same task as the browser's event (WebAssembly) can see the second toggle undo the first.
    private async Task<bool> AssignIsOpenInternal(bool value)
    {
        _internalIsOpenChange = true;
        try
        {
            return await AssignIsOpen(value);
        }
        finally
        {
            _internalIsOpenChange = false;
        }
    }

    // A menu that closes ends the search that was being typed into it, so the next one starts over rather
    // than continuing a string the user can no longer see the matches of.
    private void ResetTypeahead()
    {
        _typeahead.Reset();
        _typeaheadCalloutId = string.Empty;
    }

    // The loading state guards the click from the moment it starts, but the spinner is held back for
    // LoadingDelay - long enough that an operation which returns immediately never flashes one.
    private void UpdateLoadingVisuals()
    {
        if (Loading)
        {
            if (_showLoading || _loadingDelayCts is not null) return;

            if (LoadingDelay < 1)
            {
                _showLoading = true;
                return;
            }

            _loadingDelayCts = new();
            _ = ShowLoadingAfterDelay(_loadingDelayCts.Token);
        }
        else
        {
            _showLoading = false;
            CancelLoadingDelay();
        }
    }

    private async Task ShowLoadingAfterDelay(CancellationToken token)
    {
        try
        {
            await Task.Delay(LoadingDelay, token);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        if (IsDisposed || Loading is false || token.IsCancellationRequested) return;

        _showLoading = true;

        await InvokeAsync(StateHasChanged);
    }

    private void CancelLoadingDelay()
    {
        if (_loadingDelayCts is null) return;

        _loadingDelayCts.Cancel();
        _loadingDelayCts.Dispose();
        _loadingDelayCts = null;
    }

    private async Task ToggleCallout()
    {
        // What a menu the pointer opened registered for the keyboard goes with it, however it is closed.
        if (IsOpen is false)
        {
            await DisposeHoverKeyboard();
        }

        if (Disabled || IsDisposed) return;

        // Only an open state the page restyles the root for can move what the menu is placed against.
        _repositionAfterRender = IsOpen && (Styles?.Opened.HasValue() is true || Classes?.Opened.HasValue() is true);

        // A menu opened at a point is placed against the zero-sized element rendered there, so the placement code
        // needs to know nothing about points: it measures an element as it always does.
        var componentId = HasPoint ? _pointId : _Id;

        if (IsOpen is false)
        {
            // Whatever opens the menu next places it against the button, unless it is OpenAt again.
            ForgetPoint();
        }
        else if (HasPoint is false)
        {
            // An origin remembered for a menu that was opened at a point and then dismissed from outside (a click on
            // the page) was never handed back, and is not where the focus of a menu opened from the button goes.
            _focusOriginStored = false;
        }

        await _js.BitCalloutToggleCallout(
            dotnetObj: _dotnetObj,
            componentId: componentId,
            component: null,
            calloutId: _calloutId,
            callout: null,
            // An empty id tells the JS side that no overlay takes the outside clicks, so that the page-level
            // handler dismisses the menu instead (see NoOverlay).
            overlayId: HasOverlay ? _overlayId : "",
            isCalloutOpen: IsOpen,
            // A sheet coming up from the bottom edge, the way an action sheet does, rather than a side panel: a menu
            // is a short list of actions within reach of the thumb.
            responsiveMode: Responsive ? BitResponsiveMode.Bottom : BitResponsiveMode.None,
            dropDirection: DropDirection,
            isRtl: ExplicitRtl,
            // With nothing else named as the scrollable part, the callout itself takes that role, so that a menu
            // taller than the screen scrolls inside the callout instead of running off the bottom of it - where a
            // fixed-positioned element leaves it out of reach of the page's own scrolling. An author-set MaxHeight
            // is a cap of its own (a CSS custom property the stylesheet reads), so the fitting pass steps aside.
            scrollContainerId: MaxHeight.HasValue() ? "" : _calloutId,
            scrollOffset: 0,
            headerId: "",
            footerId: "",
            setCalloutWidth: true,
            fixedCalloutWidth: false,
            maxWindowWidth: 0,
            // The root is named so that what it declares - the custom properties of Style and Styles.Root,
            // and ForceAnimation's bit-fam - goes on reaching the callout once it is relocated to the body.
            rootId: _Id);
    }

    private void OnSetIsOpen()
    {
        // Every way of closing the menu goes through here, so this is where the next opening starts over from a menu
        // nobody owns: whatever opens it names its owner (see OpenCallout).
        if (IsOpen is false)
        {
            _owner = CalloutOwner.Default;

            // A submenu the pointer was about to open (or close) beside a row belongs to the menu that just closed,
            // so its ticket goes stale here rather than opening a submenu out of a menu that is no longer showing.
            NextHoverToken();
        }

        // The component's own open and close flows toggle the callout themselves, right after the assignment.
        if (_internalIsOpenChange) return;

        _ = ToggleCalloutOnSetIsOpen();
    }

    // The page closing the menu by writing IsOpen back is the menu closing, so the submenus go with it -
    // from the inside out, and before the menu itself, which is what each of them was moved out of.
    private async Task ToggleCalloutOnSetIsOpen()
    {
        if (IsOpen is false)
        {
            await CloseSubmenusFrom(0);
        }

        await ToggleCallout();
    }

    private void OnSetSelectedItem()
    {
        // The selected item affects both the sticky header (rendered by this component) and the
        // items rendered by the options themselves, so re-render all of them.
        RefreshOptions();
        StateHasChanged();
    }

    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitMenuButtonOption)?.InternalStateHasChanged();
        }
    }

    // The pointer arriving at the menu button, at its menu or at one of its submenus in the OpenOnHover mode. Each of
    // them is a place the pointer can be on its way between the others, so arriving at any one calls off the close
    // that leaving the last one scheduled - which is what keeps the menu from flickering as the pointer crosses the
    // gap between the button and the menu, or between a row and the submenu beside it.
    internal async Task HandleOnMouseEnter()
    {
        if (HoverDriven is false) return;

        _hoverInside = true;

        CancelHover();

        // The loading state takes the click of the header of a plain menu button, so it takes the hover that stands
        // in for that click too; the chevron of a split one still opens the menu.
        if (Disabled || (Split is false && Loading) || IsOpen) return;

        if (await DelayHover(HoverOpenDelay) is false) return;

        // The state may have moved on while the delay ran: the menu opened some other way (a click, a key, IsOpen),
        // the menu button disabled or loading, the hover mode turned off.
        if (HoverDriven is false || _hoverInside is false || Disabled || (Split is false && Loading) || IsOpen) return;

        if (await OpenCallout(CalloutOwner.Pointer) is false) return;

        StateHasChanged();
    }

    internal async Task HandleOnMouseLeave()
    {
        if (HoverDriven is false) return;

        _hoverInside = false;

        CancelHover();

        // A menu the keyboard is in is the keyboard's to close: the pointer only drifting off the button must not
        // take it away from under the focus.
        if (Disabled || IsOpen is false || _owner is CalloutOwner.Keyboard) return;

        if (await DelayHover(HoverCloseDelay) is false) return;

        // The pointer came back before the delay was up, onto the button or into one of the menus; or the menu was
        // closed, or handed to the keyboard, while the delay ran.
        if (_hoverInside || IsOpen is false || _owner is CalloutOwner.Keyboard) return;

        await CloseCallout();

        StateHasChanged();
    }

    // The hover mode only applies to the devices that have a pointer to hover with: a tap on a touch screen reports a
    // mouseover of its own, which would fight the click that is meant to toggle the menu.
    internal bool HoverDriven => OpenOnHover && _isHoverDevice is true;

    // The attributes of the root. The pointer is only listened to in the OpenOnHover mode: every other menu button
    // would be paying for a round trip per hover, and a re-render after it, and have nothing to do with it. In that
    // mode a mouseenter or mouseleave handler the page splatted on still runs, before the menu button's own.
    private Dictionary<string, object> GetRootAttributes()
    {
        var attributes = DisabledAwareHtmlAttributes;

        if (HoverDriven is false) return attributes;

        object? pageEnter = null;
        object? pageLeave = null;
        var merged = new Dictionary<string, object>(attributes.Count + 2);

        foreach (var (name, value) in attributes)
        {
            // The render tree matches an attribute name without regard to case, so a differently cased spelling of
            // the page's would be overwritten all the same.
            if (string.Equals(name, "onmouseenter", StringComparison.OrdinalIgnoreCase))
            {
                pageEnter = value;
            }
            else if (string.Equals(name, "onmouseleave", StringComparison.OrdinalIgnoreCase))
            {
                pageLeave = value;
            }
            else
            {
                merged[name] = value;
            }
        }

        merged["onmouseenter"] = EventCallback.Factory.Create<MouseEventArgs>(this, async e =>
        {
            await InvokePageHandler(pageEnter, e);
            await HandleOnMouseEnter();
        });
        merged["onmouseleave"] = EventCallback.Factory.Create<MouseEventArgs>(this, async e =>
        {
            await InvokePageHandler(pageLeave, e);
            await HandleOnMouseLeave();
        });

        return merged;
    }

    // The pointer handlers of the menu, null outside the OpenOnHover mode for the reason the root's are (see
    // GetRootAttributes).
    private Dictionary<string, object>? GetCalloutHoverAttributes()
    {
        if (HoverDriven is false) return null;

        return new()
        {
            ["onmouseenter"] = EventCallback.Factory.Create<MouseEventArgs>(this, HandleOnMouseEnter),
            ["onmouseleave"] = EventCallback.Factory.Create<MouseEventArgs>(this, HandleOnMouseLeave)
        };
    }

    // Runs a handler the page splatted on the root the way the renderer would have: an EventCallback as it is, and a
    // delegate with the component it belongs to as the receiver, so that component re-renders after it. A string -
    // inline script - cannot run beside a handler of the menu button's own, the browser taking one or the other.
    private static Task InvokePageHandler(object? handler, MouseEventArgs e) => handler switch
    {
        EventCallback<MouseEventArgs> callback => callback.InvokeAsync(e),
        EventCallback callback => callback.InvokeAsync(e),
        MulticastDelegate @delegate => new EventCallback(@delegate.Target as IHandleEvent, @delegate).InvokeAsync(e),
        _ => Task.CompletedTask
    };

    // The overlay takes the clicks of the page while the menu is open. NoOverlay leaves the page its own, and so does
    // the hover mode, where the overlay would also cover the button the pointer opened the menu from and take it
    // away from under the pointer. Either way the clicks outside the menu are left to the page-level handler of the
    // JS side, which dismisses it.
    private bool HasOverlay => NoOverlay is false && HoverDriven is false;

    private async Task<bool> GetIsHoverDevice()
    {
        try
        {
            return await _js.BitUtilsIsHoverDevice();
        }
        // A device that cannot be asked is taken for one without a pointer to hover with, which leaves the menu button
        // toggling on a click as it does without the mode.
        catch (JSException) { return false; }
        catch (JSDisconnectedException) { return false; }
        catch (TaskCanceledException) { return false; }
    }

    private void CancelHover()
    {
        var cts = _hoverCts;
        if (cts is null) return;

        _hoverCts = null;
        cts.Cancel();
        cts.Dispose();
    }

    // Waits out the hover delay and reports whether the wait is still the one that matters: the pointer moving again
    // cancels it, and the menu button may be gone by the time it is over.
    private async Task<bool> DelayHover(int delay)
    {
        if (delay <= 0) return IsDisposed is false;

        var cts = new CancellationTokenSource();
        _hoverCts = cts;

        try
        {
            await Task.Delay(delay, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }

        if (ReferenceEquals(_hoverCts, cts) is false) return false;

        _hoverCts = null;
        cts.Dispose();

        return IsDisposed is false;
    }

    // A menu the pointer opened is shown wherever the focus happens to be, and content that appears on hover has to be
    // dismissible without moving the pointer or the focus (WCAG 1.4.13), so an Escape pressed anywhere outside it and
    // the root closes it as well; and the focus going into it other than by a press of the pointer is watched for,
    // since that is the keyboard taking it over. It is the "focusIn" mode BitDropMenu registers for a callout the
    // pointer opened (see Callouts.setupMenuKeyboard). The menu's own keys are its own once the keyboard holds it.
    private async Task SetupHoverKeyboard()
    {
        if (IsDisposed || IsOpen is false || _dotnetObj is null) return;

        _hoverKeyboardRegistered = true;

        try
        {
            await _js.BitCalloutSetupMenuKeyboard(_calloutId, _dotnetObj, "focusIn", _Id + (Split ? "-chb" : "-opb"), _Id, focusFirst: false);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    private async Task DisposeHoverKeyboard()
    {
        if (_hoverKeyboardRegistered is false) return;

        _hoverKeyboardRegistered = false;

        try
        {
            await _js.BitCalloutDisposeKeyboard(_calloutId, forgetFocusOrigin: false);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here
    }

    // The keyboard taking over a menu the pointer opened: an arrow key, Enter or Space on the trigger, a key pressed in
    // the menu, or the focus going into it other than by a press of the pointer. From then on it is handled as one the
    // keyboard opened: the pointer leaving it no longer closes it, the close the pointer may already have scheduled is
    // called off, and its Escape is only the one pressed in it or on its trigger.
    private async Task TakeOverFromPointer()
    {
        if (IsOpen is false || _owner is not CalloutOwner.Pointer) return;

        _owner = CalloutOwner.Keyboard;

        CancelHover();

        await DisposeHoverKeyboard();
    }

    // The current item is read off the URL, so a navigation re-renders the menus that have one to mark - the items the
    // menu button renders itself, and the options that render their own.
    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        if (IsDisposed) return;

        // The items are read on the renderer's dispatcher, where they are changed too: the event is raised wherever
        // the navigation happened. Whether any of them has a Match is read once per change of the items rather than
        // walked again on every navigation by every menu button of the page.
        _ = InvokeAsync(() =>
        {
            if (IsDisposed) return;

            _hasMatches ??= EnumerateItems().Any(i => GetMatch(i) is not null);

            if (_hasMatches is false) return;

            RefreshOptions();
            StateHasChanged();
        });
    }

    internal string GetItemKey(TItem item, string defaultKey)
    {
        return GetKey(item) ?? $"{UniqueId}-{defaultKey}";
    }

    private string? GetCalloutCss()
    {
        var css = GetMenuCalloutCss(IsOpen);

        // Only the menu the button opens becomes the sheet; a submenu stays a callout beside its item.
        if (Responsive)
        {
            css = $"bit-mnb-res {css}";
        }

        // Only the menu the button opens is capped by MaxHeight; a submenu fits itself to the viewport.
        return MaxHeight.HasValue() ? $"bit-mnb-mxh {css}" : css;
    }

    // What every menu the button renders carries: the open state, and the classes the callout cannot
    // inherit because it is a sibling of the root element rather than a descendant of it.
    internal string? GetMenuCalloutCss(bool isOpen)
    {
        List<string> classes = [];

        if (isOpen)
        {
            classes.Add("bit-mnb-ocl");
        }

        // The callout is a sibling of the root, so the root's bit-fam class never reaches it where it is
        // rendered - only while it is relocated, through the copy of the root Callouts.ts moves it into. So
        // ForceAnimation is rendered on the callout itself, for its motion to opt out of reduced motion
        // whether or not it is relocated.
        if (ForceAnimation)
        {
            classes.Add("bit-fam");
        }

        // The callout is rendered outside the root element - and moved to the body while it is open - so it is
        // a sibling of the root rather than a descendant of it. Callouts.ts carries the custom properties and the
        // author's classes of the root into it, but none of the component's own bit- classes, so the two that
        // carry what the items need are repeated here: the size class sizes their text, their height and their
        // padding, and the color class paints the focus ring of the focused one and the glyph of a checked one in
        // the color the menu button was given. Like the root, the callout carries neither while it is unset, so
        // the public variables restyle the default there and never an explicit value.
        classes.AddIfHasValue(BitCssClasses.Color(Color, "bit-mnb"));
        classes.AddIfHasValue(BitCssClasses.Size(Size, "bit-mnb"));

        var bgClass = Background switch
        {
            BitColorKind.Primary => "bit-mnb-bpg",
            BitColorKind.Secondary => "bit-mnb-bsg",
            BitColorKind.Tertiary => "bit-mnb-btg",
            BitColorKind.Transparent => "bit-mnb-brg",
            _ => null
        };

        if (bgClass is not null)
        {
            classes.Add(bgClass);
        }

        var result = string.Join(' ', classes).Trim();
        return result.HasValue() ? result : null;
    }

    private string? GetCalloutStyle()
    {
        // The positioning code clears the callout's inline sizing on every layout pass, so the cap travels as a
        // custom property the stylesheet reads instead of as a max-height of its own.
        var maxHeight = MaxHeight.HasValue() ? $"--bit-MenuButton-callout-max-height:{MaxHeight};" : null;

        var result = $"{maxHeight}{Styles?.Callout}";
        return result.HasValue() ? result : null;
    }

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        CancelLoadingDelay();

        CancelHover();

        _navigationManager.LocationChanged -= OnLocationChanged;

        await base.DisposeAsync(disposing);

        if (_dotnetObj is not null)
        {
            // The callout still calls back through the reference until it is cleared.
            await _dotnetObj.DisposeAfterAsync(async () =>
            {
                await _js.BitCalloutClearCallout(_calloutId);
                await _js.BitMenuButtonsDispose(_Id);

                // Only what is registered is released: the keyboard handling of a menu the pointer opened exists
                // while it is open.
                await DisposeHoverKeyboard();

                // An origin that was never handed back would keep its element alive for as long as the page lives.
                if (_focusOriginStored)
                {
                    await _js.BitUtilsForgetFocus(_calloutId);
                }
            });
        }
    }



    // Who the open menu answers to. The pointer owns one it opened by hovering until the keyboard asks for it (see
    // TakeOverFromPointer); the keyboard owns one it opened or took over, and the pointer leaving that one does not
    // close it. A menu opened any other way - a click, the IsOpen parameter - is the default one. Closing the menu
    // resets it (see OnSetIsOpen).
    private enum CalloutOwner
    {
        Default,
        Pointer,
        Keyboard
    }
}
