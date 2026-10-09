namespace Bit.BlazorUI;

/// <summary>
/// The parameters for <see cref="BitNavPanel{TItem}"/> component.
/// </summary>
/// <remarks>
/// What belongs to a single panel rather than to a group of them is left out on purpose: its items, the state
/// it two-way binds (IsOpen, IsToggled, SearchText and SelectedItem), the initial selection, the templates and
/// the selectors typed over its items, the search filter, and its event callbacks.
/// <br />
/// None of the parameters here depends on the type of the items, so the class is not generic: one instance
/// reaches every panel under the <see cref="BitParams"/> it is given to, whatever its item type is - which is
/// what makes it the one place a localized app names the panel's controls and announcements.
/// </remarks>
public class BitNavPanelParams : BitComponentBaseParams, IBitComponentParams
{
    /// <summary>
    /// Represents the parameter name used to identify the <see cref="BitNavPanel{TItem}"/> cascading parameters within <see cref="BitParams"/>.
    /// </summary>
    /// <remarks>
    /// This constant is typically used when referencing or accessing the BitNavPanel value in
    /// parameterized APIs or configuration settings. Using this constant helps ensure consistency and reduces the risk
    /// of typographical errors.
    /// </remarks>
    public const string ParamName = $"{nameof(BitParams)}.{nameof(BitNavPanel<object>)}";



    public string Name => ParamName;



    /// <summary>
    /// The accent color of the nav, which also paints the background of the panel.
    /// </summary>
    public BitColor? Accent { get; set; }

    /// <summary>
    /// Expands all items on first render.
    /// </summary>
    public bool? AllExpanded { get; set; }

    /// <summary>
    /// Moves the focus onto the search box of the drawer as it opens - or onto the first item of a panel
    /// without one - instead of onto the drawer itself, where a modal drawer puts it otherwise.
    /// It also moves the focus into a drawer that does not trap it (NoFocusTrap, NoOverlay), which takes none
    /// of its own.
    /// </summary>
    public bool? AutoFocus { get; set; }

    /// <summary>
    /// The icon for the chevron-down element of each nav item, from an external icon library.
    /// Takes precedence over <see cref="ChevronDownIconName"/> when both are set, and is not applied to a panel
    /// that sets its own ChevronDownIcon or ChevronDownIconName.
    /// </summary>
    public BitIconInfo? ChevronDownIcon { get; set; }

    /// <summary>
    /// The custom icon name of the chevron-down element of each nav item.
    /// </summary>
    public string? ChevronDownIconName { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the nav panel.
    /// </summary>
    public BitNavPanelClassStyles? Classes { get; set; }

    /// <summary>
    /// The aria-label and the tooltip of the close button of the nav panel.
    /// </summary>
    public string? CloseAriaLabel { get; set; }

    /// <summary>
    /// The icon of the close button of the nav panel, from an external icon library.
    /// Takes precedence over <see cref="CloseIconName"/> when both are set, and is not applied to a panel that
    /// sets its own CloseIcon or CloseIconName.
    /// </summary>
    public BitIconInfo? CloseIcon { get; set; }

    /// <summary>
    /// The name of the icon of the close button of the nav panel.
    /// </summary>
    public string? CloseIconName { get; set; }

    /// <summary>
    /// The default aria-label of the expand/collapse button of an expanded item of the nav.
    /// </summary>
    public string? CollapseAriaLabel { get; set; }

    /// <summary>
    /// The general color of the nav.
    /// </summary>
    public BitColor? Color { get; set; }

    /// <summary>
    /// The screen width below which the nav panel turns into an off-canvas drawer.
    /// </summary>
    public BitNavPanelBreakpoint? DrawerBreakpoint { get; set; }

    /// <summary>
    /// The custom message for when the search result is empty.
    /// </summary>
    public string? EmptyListMessage { get; set; }

    /// <summary>
    /// The custom template for when the search result is empty.
    /// </summary>
    public RenderFragment? EmptyListTemplate { get; set; }

    /// <summary>
    /// The default aria-label of the expand/collapse button of a collapsed item of the nav.
    /// </summary>
    public string? ExpandAriaLabel { get; set; }

    /// <summary>
    /// Expands the toggled (rail) nav panel back to its full width while the pointer or the keyboard is inside it.
    /// </summary>
    public bool? ExpandOnHover { get; set; }

    /// <summary>
    /// Renders the nav panel with fit-content width.
    /// </summary>
    public bool? FitWidth { get; set; }

    /// <summary>
    /// The custom template to render as the footer of the nav panel.
    /// </summary>
    public RenderFragment? Footer { get; set; }

    /// <summary>
    /// Renders the nav panel with full (100%) width.
    /// </summary>
    public bool? FullWidth { get; set; }

    /// <summary>
    /// The custom template to render as the header of the nav panel, in place of the logo and the buttons.
    /// </summary>
    public RenderFragment? Header { get; set; }

    /// <summary>
    /// The title shown beside the logo in the header of the nav panel - typically the name of the app.
    /// </summary>
    public string? HeaderText { get; set; }

    /// <summary>
    /// The render mode of the custom HeaderTemplate of the nav.
    /// </summary>
    public BitNavItemTemplateRenderMode? HeaderTemplateRenderMode { get; set; }

    /// <summary>
    /// Removes the toggle button.
    /// </summary>
    public bool? HideToggle { get; set; }

    /// <summary>
    /// The accessible name of the logo in the header of the nav panel.
    /// </summary>
    public string? IconAriaLabel { get; set; }

    /// <summary>
    /// Renders an anchor wrapping the icon to navigate to the specified url.
    /// </summary>
    public string? IconNavUrl { get; set; }

    /// <summary>
    /// The icon url to show in the header of the nav panel.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// The width in px of the chevron, which the items without children keep as padding in its place.
    /// </summary>
    public int? IndentPadding { get; set; }

    /// <summary>
    /// The indentation padding in px for items in reversed mode.
    /// </summary>
    public int? IndentReversedPadding { get; set; }

    /// <summary>
    /// The indentation value in px for each level of depth of child item.
    /// </summary>
    public int? IndentValue { get; set; }

    /// <summary>
    /// The render mode of the custom ItemTemplate.
    /// </summary>
    public BitNavItemTemplateRenderMode? ItemTemplateRenderMode { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the nav component of the nav panel.
    /// </summary>
    public BitNavClassStyles? NavClasses { get; set; }

    /// <summary>
    /// Determines the global URL matching behavior of the nav.
    /// </summary>
    public BitNavMatch? NavMatch { get; set; }

    /// <summary>
    /// Determines how the navigation will be handled.
    /// </summary>
    public BitNavMode? NavMode { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav component of the nav panel.
    /// </summary>
    public BitNavClassStyles? NavStyles { get; set; }

    /// <summary>
    /// Keeps the nav panel open when an item with a URL is clicked, instead of closing it.
    /// </summary>
    public bool? NoAutoClose { get; set; }

    /// <summary>
    /// Keeps every item of the nav expanded and hides its collapse/expand buttons.
    /// </summary>
    public bool? NoCollapse { get; set; }

    /// <summary>
    /// Stops the open drawer of a small screen from taking the focus as it opens and holding it inside itself.
    /// Left false, every modal drawer under the <see cref="BitParams"/> takes the focus onto itself as it opens
    /// (or AutoFocus moves it further in), and hands it back as it closes.
    /// </summary>
    public bool? NoFocusTrap { get; set; }

    /// <summary>
    /// Removes the overlay that is rendered behind the open nav panel in small screens.
    /// </summary>
    public bool? NoOverlay { get; set; }

    /// <summary>
    /// Disables the padded mode of the nav panel.
    /// </summary>
    public bool? NoPad { get; set; }

    /// <summary>
    /// Stops the closing drawer of a small screen from handing the focus back to the element that had it
    /// when the drawer opened. Only ever read by a panel that took the focus in the first place: a modal
    /// drawer, or one with AutoFocus.
    /// </summary>
    public bool? NoRestoreFocus { get; set; }

    /// <summary>
    /// Lets the page behind the open drawer of a small screen keep scrolling.
    /// </summary>
    public bool? NoScrollLock { get; set; }

    /// <summary>
    /// Removes the search box from the nav panel.
    /// </summary>
    public bool? NoSearchBox { get; set; }

    /// <summary>
    /// Disables the swipe gesture that closes the open nav panel in small screens.
    /// </summary>
    public bool? NoSwipe { get; set; }

    /// <summary>
    /// Disables the toggle feature of the nav panel.
    /// </summary>
    public bool? NoToggle { get; set; }

    /// <summary>
    /// The edge the off-canvas drawer of a small screen comes from. Start and End are the honoured values;
    /// they follow the reading direction, and every other value renders the default Start.
    /// </summary>
    public BitPlacement? Placement { get; set; }

    /// <summary>
    /// The way to render nav items.
    /// </summary>
    public BitNavRenderType? RenderType { get; set; }

    /// <summary>
    /// Enables recalling the select events when the same item is selected.
    /// </summary>
    public bool? Reselectable { get; set; }

    /// <summary>
    /// Reverses the location of the expander chevron.
    /// </summary>
    public bool? ReversedChevron { get; set; }

    /// <summary>
    /// Builds the text the screen reader announces whenever the search filters the items, from the number of
    /// items it matched.
    /// </summary>
    public Func<int, string?>? SearchAnnouncementProvider { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the search box of the nav panel.
    /// </summary>
    public BitSearchBoxClassStyles? SearchBoxClasses { get; set; }

    /// <summary>
    /// The placeholder of the input element of the search box of the nav panel.
    /// </summary>
    public string? SearchBoxPlaceholder { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the search box of the nav panel.
    /// </summary>
    public BitSearchBoxClassStyles? SearchBoxStyles { get; set; }

    /// <summary>
    /// The debounce time in milliseconds of the search box of the nav panel.
    /// </summary>
    public int? SearchDebounceTime { get; set; }

    /// <summary>
    /// The icon of the button that the collapsed (rail) nav panel shows in place of its search box, from an
    /// external icon library. Takes precedence over <see cref="SearchIconName"/> when both are set, and is not
    /// applied to a panel that sets its own SearchIcon or SearchIconName.
    /// </summary>
    public BitIconInfo? SearchIcon { get; set; }

    /// <summary>
    /// The name of the icon of the button that the collapsed (rail) nav panel shows in place of its search box.
    /// </summary>
    public string? SearchIconName { get; set; }

    /// <summary>
    /// Renders a close button in the header of the nav panel, on the screens the panel is an off-canvas drawer on.
    /// </summary>
    public bool? ShowCloseButton { get; set; }

    /// <summary>
    /// Enables the single-expand mode in the BitNav.
    /// </summary>
    public bool? SingleExpand { get; set; }

    /// <summary>
    /// The size of the nav items.
    /// </summary>
    public BitSize? Size { get; set; }

    /// <summary>
    /// Pins the header and the footer of the nav panel in place and scrolls only the items between them.
    /// </summary>
    public bool? StickyEnds { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the nav panel.
    /// </summary>
    public BitNavPanelClassStyles? Styles { get; set; }

    /// <summary>
    /// The aria-label of the toggle button of the nav panel.
    /// </summary>
    public string? ToggleAriaLabel { get; set; }

    /// <summary>
    /// The width of the nav panel in px in its toggled (rail) state.
    /// </summary>
    public int? ToggledWidth { get; set; }

    /// <summary>
    /// The icon of the toggle button of the nav panel, from an external icon library.
    /// Takes precedence over <see cref="ToggleIconName"/> when both are set, and is not applied to a panel that
    /// sets its own ToggleIcon or ToggleIconName.
    /// </summary>
    public BitIconInfo? ToggleIcon { get; set; }

    /// <summary>
    /// The name of the icon of the toggle button of the nav panel.
    /// </summary>
    public string? ToggleIconName { get; set; }

    /// <summary>
    /// The top CSS property value of the root element of the nav panel in px.
    /// </summary>
    public int? Top { get; set; }

    /// <summary>
    /// The width of the nav panel in px. It is ignored in the FitWidth and FullWidth modes.
    /// </summary>
    public int? Width { get; set; }



    /// <summary>
    /// Updates the properties of the specified <see cref="BitNavPanel{TItem}"/> instance with any values that have been set on
    /// this object, if those properties have not already been set on the <see cref="BitNavPanel{TItem}"/>.
    /// </summary>
    /// <remarks>
    /// Only properties that have a value set and have not already been set on the <paramref name="bitNavPanel"/> will be updated.
    /// This method does not overwrite existing values on <paramref name="bitNavPanel"/>.
    /// </remarks>
    /// <param name="bitNavPanel">
    /// The <see cref="BitNavPanel{TItem}"/> instance whose properties will be updated. Cannot be null.
    /// </param>
    public void UpdateParameters<TItem>(BitNavPanel<TItem> bitNavPanel) where TItem : class
    {
        if (bitNavPanel is null) return;

        UpdateBaseParameters(bitNavPanel);

        if (Accent.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(Accent), Accent.Value, static n => n.Accent, static (n, v) => n.Accent = v);
        }

        if (AllExpanded.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(AllExpanded), AllExpanded.Value, static n => n.AllExpanded, static (n, v) => n.AllExpanded = v);
        }

        if (AutoFocus.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(AutoFocus), AutoFocus.Value, static n => n.AutoFocus, static (n, v) => n.AutoFocus = v);
        }

        var ownChevronDownIcon = bitNavPanel.HasSetAnyOf(nameof(ChevronDownIcon), nameof(ChevronDownIconName));

        if (ChevronDownIcon is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(ChevronDownIcon), ChevronDownIcon, static n => n.ChevronDownIcon, static (n, v) => n.ChevronDownIcon = v, outranked: ownChevronDownIcon);
        }

        if (ChevronDownIconName.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(ChevronDownIconName), ChevronDownIconName, static n => n.ChevronDownIconName, static (n, v) => n.ChevronDownIconName = v, outranked: ownChevronDownIcon);
        }

        if (Classes is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(Classes), Classes, static n => n.Classes, static (n, v) => n.Classes = v);
        }

        if (CloseAriaLabel.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(CloseAriaLabel), CloseAriaLabel, static n => n.CloseAriaLabel, static (n, v) => n.CloseAriaLabel = v);
        }

        var ownCloseIcon = bitNavPanel.HasSetAnyOf(nameof(CloseIcon), nameof(CloseIconName));

        if (CloseIcon is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(CloseIcon), CloseIcon, static n => n.CloseIcon, static (n, v) => n.CloseIcon = v, outranked: ownCloseIcon);
        }

        if (CloseIconName.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(CloseIconName), CloseIconName, static n => n.CloseIconName, static (n, v) => n.CloseIconName = v, outranked: ownCloseIcon);
        }

        if (CollapseAriaLabel.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(CollapseAriaLabel), CollapseAriaLabel, static n => n.CollapseAriaLabel, static (n, v) => n.CollapseAriaLabel = v);
        }

        if (Color.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(Color), Color.Value, static n => n.Color, static (n, v) => n.Color = v);
        }

        if (DrawerBreakpoint.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(DrawerBreakpoint), DrawerBreakpoint.Value, static n => n.DrawerBreakpoint, static (n, v) => n.DrawerBreakpoint = v);
        }

        if (EmptyListMessage.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(EmptyListMessage), EmptyListMessage, static n => n.EmptyListMessage, static (n, v) => n.EmptyListMessage = v);
        }

        if (EmptyListTemplate is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(EmptyListTemplate), EmptyListTemplate, static n => n.EmptyListTemplate, static (n, v) => n.EmptyListTemplate = v);
        }

        if (ExpandAriaLabel.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(ExpandAriaLabel), ExpandAriaLabel, static n => n.ExpandAriaLabel, static (n, v) => n.ExpandAriaLabel = v);
        }

        if (ExpandOnHover.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(ExpandOnHover), ExpandOnHover.Value, static n => n.ExpandOnHover, static (n, v) => n.ExpandOnHover = v);
        }

        if (FitWidth.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(FitWidth), FitWidth.Value, static n => n.FitWidth, static (n, v) => n.FitWidth = v);
        }

        if (Footer is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(Footer), Footer, static n => n.Footer, static (n, v) => n.Footer = v);
        }

        if (FullWidth.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(FullWidth), FullWidth.Value, static n => n.FullWidth, static (n, v) => n.FullWidth = v);
        }

        if (Header is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(Header), Header, static n => n.Header, static (n, v) => n.Header = v);
        }

        if (HeaderText.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(HeaderText), HeaderText, static n => n.HeaderText, static (n, v) => n.HeaderText = v);
        }

        if (HeaderTemplateRenderMode.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(HeaderTemplateRenderMode), HeaderTemplateRenderMode.Value, static n => n.HeaderTemplateRenderMode, static (n, v) => n.HeaderTemplateRenderMode = v);
        }

        if (HideToggle.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(HideToggle), HideToggle.Value, static n => n.HideToggle, static (n, v) => n.HideToggle = v);
        }

        if (IconAriaLabel.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(IconAriaLabel), IconAriaLabel, static n => n.IconAriaLabel, static (n, v) => n.IconAriaLabel = v);
        }

        if (IconNavUrl.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(IconNavUrl), IconNavUrl, static n => n.IconNavUrl, static (n, v) => n.IconNavUrl = v);
        }

        if (IconUrl.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(IconUrl), IconUrl, static n => n.IconUrl, static (n, v) => n.IconUrl = v);
        }

        if (IndentPadding.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(IndentPadding), IndentPadding.Value, static n => n.IndentPadding, static (n, v) => n.IndentPadding = v);
        }

        if (IndentReversedPadding.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(IndentReversedPadding), IndentReversedPadding.Value, static n => n.IndentReversedPadding, static (n, v) => n.IndentReversedPadding = v);
        }

        if (IndentValue.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(IndentValue), IndentValue.Value, static n => n.IndentValue, static (n, v) => n.IndentValue = v);
        }

        if (ItemTemplateRenderMode.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(ItemTemplateRenderMode), ItemTemplateRenderMode.Value, static n => n.ItemTemplateRenderMode, static (n, v) => n.ItemTemplateRenderMode = v);
        }

        if (NavClasses is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(NavClasses), NavClasses, static n => n.NavClasses, static (n, v) => n.NavClasses = v);
        }

        if (NavMatch.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NavMatch), NavMatch.Value, static n => n.NavMatch, static (n, v) => n.NavMatch = v);
        }

        if (NavMode.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NavMode), NavMode.Value, static n => n.NavMode, static (n, v) => n.NavMode = v);
        }

        if (NavStyles is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(NavStyles), NavStyles, static n => n.NavStyles, static (n, v) => n.NavStyles = v);
        }

        if (NoAutoClose.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoAutoClose), NoAutoClose.Value, static n => n.NoAutoClose, static (n, v) => n.NoAutoClose = v);
        }

        if (NoCollapse.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoCollapse), NoCollapse.Value, static n => n.NoCollapse, static (n, v) => n.NoCollapse = v);
        }

        if (NoFocusTrap.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoFocusTrap), NoFocusTrap.Value, static n => n.NoFocusTrap, static (n, v) => n.NoFocusTrap = v);
        }

        if (NoOverlay.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoOverlay), NoOverlay.Value, static n => n.NoOverlay, static (n, v) => n.NoOverlay = v);
        }

        if (NoPad.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoPad), NoPad.Value, static n => n.NoPad, static (n, v) => n.NoPad = v);
        }

        if (NoRestoreFocus.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoRestoreFocus), NoRestoreFocus.Value, static n => n.NoRestoreFocus, static (n, v) => n.NoRestoreFocus = v);
        }

        if (NoScrollLock.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoScrollLock), NoScrollLock.Value, static n => n.NoScrollLock, static (n, v) => n.NoScrollLock = v);
        }

        if (NoSearchBox.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoSearchBox), NoSearchBox.Value, static n => n.NoSearchBox, static (n, v) => n.NoSearchBox = v);
        }

        if (NoSwipe.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoSwipe), NoSwipe.Value, static n => n.NoSwipe, static (n, v) => n.NoSwipe = v);
        }

        if (NoToggle.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(NoToggle), NoToggle.Value, static n => n.NoToggle, static (n, v) => n.NoToggle = v);
        }

        if (Placement.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(Placement), Placement.Value, static n => n.Placement, static (n, v) => n.Placement = v);
        }

        if (RenderType.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(RenderType), RenderType.Value, static n => n.RenderType, static (n, v) => n.RenderType = v);
        }

        if (Reselectable.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(Reselectable), Reselectable.Value, static n => n.Reselectable, static (n, v) => n.Reselectable = v);
        }

        if (ReversedChevron.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(ReversedChevron), ReversedChevron.Value, static n => n.ReversedChevron, static (n, v) => n.ReversedChevron = v);
        }

        if (SearchAnnouncementProvider is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(SearchAnnouncementProvider), SearchAnnouncementProvider, static n => n.SearchAnnouncementProvider, static (n, v) => n.SearchAnnouncementProvider = v);
        }

        if (SearchBoxClasses is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(SearchBoxClasses), SearchBoxClasses, static n => n.SearchBoxClasses, static (n, v) => n.SearchBoxClasses = v);
        }

        if (SearchBoxPlaceholder.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(SearchBoxPlaceholder), SearchBoxPlaceholder, static n => n.SearchBoxPlaceholder, static (n, v) => n.SearchBoxPlaceholder = v);
        }

        if (SearchBoxStyles is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(SearchBoxStyles), SearchBoxStyles, static n => n.SearchBoxStyles, static (n, v) => n.SearchBoxStyles = v);
        }

        if (SearchDebounceTime.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(SearchDebounceTime), SearchDebounceTime.Value, static n => n.SearchDebounceTime, static (n, v) => n.SearchDebounceTime = v);
        }

        var ownSearchIcon = bitNavPanel.HasSetAnyOf(nameof(SearchIcon), nameof(SearchIconName));

        if (SearchIcon is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(SearchIcon), SearchIcon, static n => n.SearchIcon, static (n, v) => n.SearchIcon = v, outranked: ownSearchIcon);
        }

        if (SearchIconName.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(SearchIconName), SearchIconName, static n => n.SearchIconName, static (n, v) => n.SearchIconName = v, outranked: ownSearchIcon);
        }

        if (ShowCloseButton.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(ShowCloseButton), ShowCloseButton.Value, static n => n.ShowCloseButton, static (n, v) => n.ShowCloseButton = v);
        }

        if (SingleExpand.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(SingleExpand), SingleExpand.Value, static n => n.SingleExpand, static (n, v) => n.SingleExpand = v);
        }

        if (Size.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(Size), Size.Value, static n => n.Size, static (n, v) => n.Size = v);
        }

        if (StickyEnds.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(StickyEnds), StickyEnds.Value, static n => n.StickyEnds, static (n, v) => n.StickyEnds = v);
        }

        if (Styles is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(Styles), Styles, static n => n.Styles, static (n, v) => n.Styles = v);
        }

        if (ToggleAriaLabel.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(ToggleAriaLabel), ToggleAriaLabel, static n => n.ToggleAriaLabel, static (n, v) => n.ToggleAriaLabel = v);
        }

        if (ToggledWidth.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(ToggledWidth), ToggledWidth.Value, static n => n.ToggledWidth, static (n, v) => n.ToggledWidth = v);
        }

        var ownToggleIcon = bitNavPanel.HasSetAnyOf(nameof(ToggleIcon), nameof(ToggleIconName));

        if (ToggleIcon is not null)
        {
            bitNavPanel.TakeFromCascade(nameof(ToggleIcon), ToggleIcon, static n => n.ToggleIcon, static (n, v) => n.ToggleIcon = v, outranked: ownToggleIcon);
        }

        if (ToggleIconName.HasValue())
        {
            bitNavPanel.TakeFromCascade(nameof(ToggleIconName), ToggleIconName, static n => n.ToggleIconName, static (n, v) => n.ToggleIconName = v, outranked: ownToggleIcon);
        }

        if (Top.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(Top), Top.Value, static n => n.Top, static (n, v) => n.Top = v);
        }

        if (Width.HasValue)
        {
            bitNavPanel.TakeFromCascade(nameof(Width), Width.Value, static n => n.Width, static (n, v) => n.Width = v);
        }
    }
}
