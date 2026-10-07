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

        // This runs on every render of every panel under the BitParams, so a value that drives the class or the
        // style of the root only resets the builder when it differs from the one the panel already holds: an
        // unchanged one would rebuild both strings on every render for nothing.
        if (Accent.HasValue && bitNavPanel.HasNotBeenSet(nameof(Accent)) && bitNavPanel.Accent != Accent)
        {
            bitNavPanel.Accent = Accent.Value;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (AllExpanded.HasValue && bitNavPanel.HasNotBeenSet(nameof(AllExpanded)))
        {
            bitNavPanel.AllExpanded = AllExpanded.Value;
        }

        if (AutoFocus.HasValue && bitNavPanel.HasNotBeenSet(nameof(AutoFocus)))
        {
            bitNavPanel.AutoFocus = AutoFocus.Value;
        }

        // The icon takes precedence over the icon name, so a cascaded icon is only a default for a panel that has
        // set neither: applied over a panel's own icon name it would override it rather than default it.
        if (ChevronDownIcon is not null &&
            bitNavPanel.HasNotBeenSet(nameof(ChevronDownIcon)) &&
            bitNavPanel.HasNotBeenSet(nameof(ChevronDownIconName)))
        {
            bitNavPanel.ChevronDownIcon = ChevronDownIcon;
        }

        if (ChevronDownIconName.HasValue() && bitNavPanel.HasNotBeenSet(nameof(ChevronDownIconName)))
        {
            bitNavPanel.ChevronDownIconName = ChevronDownIconName;
        }

        if (Classes is not null && bitNavPanel.HasNotBeenSet(nameof(Classes)) && ReferenceEquals(bitNavPanel.Classes, Classes) is false)
        {
            bitNavPanel.Classes = Classes;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (CloseAriaLabel.HasValue() && bitNavPanel.HasNotBeenSet(nameof(CloseAriaLabel)))
        {
            bitNavPanel.CloseAriaLabel = CloseAriaLabel;
        }

        if (CloseIcon is not null &&
            bitNavPanel.HasNotBeenSet(nameof(CloseIcon)) &&
            bitNavPanel.HasNotBeenSet(nameof(CloseIconName)))
        {
            bitNavPanel.CloseIcon = CloseIcon;
        }

        if (CloseIconName.HasValue() && bitNavPanel.HasNotBeenSet(nameof(CloseIconName)))
        {
            bitNavPanel.CloseIconName = CloseIconName;
        }

        if (CollapseAriaLabel.HasValue() && bitNavPanel.HasNotBeenSet(nameof(CollapseAriaLabel)))
        {
            bitNavPanel.CollapseAriaLabel = CollapseAriaLabel;
        }

        if (Color.HasValue && bitNavPanel.HasNotBeenSet(nameof(Color)))
        {
            bitNavPanel.Color = Color.Value;
        }

        if (DrawerBreakpoint.HasValue && bitNavPanel.HasNotBeenSet(nameof(DrawerBreakpoint)) && bitNavPanel.DrawerBreakpoint != DrawerBreakpoint)
        {
            bitNavPanel.DrawerBreakpoint = DrawerBreakpoint.Value;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (EmptyListMessage.HasValue() && bitNavPanel.HasNotBeenSet(nameof(EmptyListMessage)))
        {
            bitNavPanel.EmptyListMessage = EmptyListMessage;
        }

        if (EmptyListTemplate is not null && bitNavPanel.HasNotBeenSet(nameof(EmptyListTemplate)))
        {
            bitNavPanel.EmptyListTemplate = EmptyListTemplate;
        }

        if (ExpandAriaLabel.HasValue() && bitNavPanel.HasNotBeenSet(nameof(ExpandAriaLabel)))
        {
            bitNavPanel.ExpandAriaLabel = ExpandAriaLabel;
        }

        if (ExpandOnHover.HasValue && bitNavPanel.HasNotBeenSet(nameof(ExpandOnHover)) && bitNavPanel.ExpandOnHover != ExpandOnHover.Value)
        {
            bitNavPanel.ExpandOnHover = ExpandOnHover.Value;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (FitWidth.HasValue && bitNavPanel.HasNotBeenSet(nameof(FitWidth)) && bitNavPanel.FitWidth != FitWidth.Value)
        {
            bitNavPanel.FitWidth = FitWidth.Value;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (Footer is not null && bitNavPanel.HasNotBeenSet(nameof(Footer)))
        {
            bitNavPanel.Footer = Footer;
        }

        if (FullWidth.HasValue && bitNavPanel.HasNotBeenSet(nameof(FullWidth)) && bitNavPanel.FullWidth != FullWidth.Value)
        {
            bitNavPanel.FullWidth = FullWidth.Value;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (Header is not null && bitNavPanel.HasNotBeenSet(nameof(Header)))
        {
            bitNavPanel.Header = Header;
        }

        if (HeaderText.HasValue() && bitNavPanel.HasNotBeenSet(nameof(HeaderText)))
        {
            bitNavPanel.HeaderText = HeaderText;
        }

        if (HeaderTemplateRenderMode.HasValue && bitNavPanel.HasNotBeenSet(nameof(HeaderTemplateRenderMode)))
        {
            bitNavPanel.HeaderTemplateRenderMode = HeaderTemplateRenderMode.Value;
        }

        if (HideToggle.HasValue && bitNavPanel.HasNotBeenSet(nameof(HideToggle)))
        {
            bitNavPanel.HideToggle = HideToggle.Value;
        }

        if (IconAriaLabel.HasValue() && bitNavPanel.HasNotBeenSet(nameof(IconAriaLabel)))
        {
            bitNavPanel.IconAriaLabel = IconAriaLabel;
        }

        if (IconNavUrl.HasValue() && bitNavPanel.HasNotBeenSet(nameof(IconNavUrl)))
        {
            bitNavPanel.IconNavUrl = IconNavUrl;
        }

        if (IconUrl.HasValue() && bitNavPanel.HasNotBeenSet(nameof(IconUrl)))
        {
            bitNavPanel.IconUrl = IconUrl;
        }

        if (IndentPadding.HasValue && bitNavPanel.HasNotBeenSet(nameof(IndentPadding)))
        {
            bitNavPanel.IndentPadding = IndentPadding.Value;
        }

        if (IndentReversedPadding.HasValue && bitNavPanel.HasNotBeenSet(nameof(IndentReversedPadding)))
        {
            bitNavPanel.IndentReversedPadding = IndentReversedPadding.Value;
        }

        if (IndentValue.HasValue && bitNavPanel.HasNotBeenSet(nameof(IndentValue)))
        {
            bitNavPanel.IndentValue = IndentValue.Value;
        }

        if (ItemTemplateRenderMode.HasValue && bitNavPanel.HasNotBeenSet(nameof(ItemTemplateRenderMode)))
        {
            bitNavPanel.ItemTemplateRenderMode = ItemTemplateRenderMode.Value;
        }

        if (NavClasses is not null && bitNavPanel.HasNotBeenSet(nameof(NavClasses)))
        {
            bitNavPanel.NavClasses = NavClasses;
        }

        if (NavMatch.HasValue && bitNavPanel.HasNotBeenSet(nameof(NavMatch)))
        {
            bitNavPanel.NavMatch = NavMatch.Value;
        }

        if (NavMode.HasValue && bitNavPanel.HasNotBeenSet(nameof(NavMode)))
        {
            bitNavPanel.NavMode = NavMode.Value;
        }

        if (NavStyles is not null && bitNavPanel.HasNotBeenSet(nameof(NavStyles)))
        {
            bitNavPanel.NavStyles = NavStyles;
        }

        if (NoAutoClose.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoAutoClose)))
        {
            bitNavPanel.NoAutoClose = NoAutoClose.Value;
        }

        if (NoCollapse.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoCollapse)))
        {
            bitNavPanel.NoCollapse = NoCollapse.Value;
        }

        if (NoFocusTrap.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoFocusTrap)))
        {
            bitNavPanel.NoFocusTrap = NoFocusTrap.Value;
        }

        if (NoOverlay.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoOverlay)))
        {
            bitNavPanel.NoOverlay = NoOverlay.Value;
        }

        if (NoPad.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoPad)) && bitNavPanel.NoPad != NoPad.Value)
        {
            bitNavPanel.NoPad = NoPad.Value;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (NoRestoreFocus.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoRestoreFocus)))
        {
            bitNavPanel.NoRestoreFocus = NoRestoreFocus.Value;
        }

        if (NoScrollLock.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoScrollLock)))
        {
            bitNavPanel.NoScrollLock = NoScrollLock.Value;
        }

        if (NoSearchBox.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoSearchBox)))
        {
            bitNavPanel.NoSearchBox = NoSearchBox.Value;
        }

        if (NoSwipe.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoSwipe)))
        {
            bitNavPanel.NoSwipe = NoSwipe.Value;
        }

        if (NoToggle.HasValue && bitNavPanel.HasNotBeenSet(nameof(NoToggle)))
        {
            bitNavPanel.NoToggle = NoToggle.Value;
        }

        if (Placement.HasValue && bitNavPanel.HasNotBeenSet(nameof(Placement)) && bitNavPanel.Placement != Placement.Value)
        {
            bitNavPanel.Placement = Placement.Value;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (RenderType.HasValue && bitNavPanel.HasNotBeenSet(nameof(RenderType)))
        {
            bitNavPanel.RenderType = RenderType.Value;
        }

        if (Reselectable.HasValue && bitNavPanel.HasNotBeenSet(nameof(Reselectable)))
        {
            bitNavPanel.Reselectable = Reselectable.Value;
        }

        if (ReversedChevron.HasValue && bitNavPanel.HasNotBeenSet(nameof(ReversedChevron)))
        {
            bitNavPanel.ReversedChevron = ReversedChevron.Value;
        }

        if (SearchAnnouncementProvider is not null && bitNavPanel.HasNotBeenSet(nameof(SearchAnnouncementProvider)))
        {
            bitNavPanel.SearchAnnouncementProvider = SearchAnnouncementProvider;
        }

        if (SearchBoxClasses is not null && bitNavPanel.HasNotBeenSet(nameof(SearchBoxClasses)))
        {
            bitNavPanel.SearchBoxClasses = SearchBoxClasses;
        }

        if (SearchBoxPlaceholder.HasValue() && bitNavPanel.HasNotBeenSet(nameof(SearchBoxPlaceholder)))
        {
            bitNavPanel.SearchBoxPlaceholder = SearchBoxPlaceholder;
        }

        if (SearchBoxStyles is not null && bitNavPanel.HasNotBeenSet(nameof(SearchBoxStyles)))
        {
            bitNavPanel.SearchBoxStyles = SearchBoxStyles;
        }

        if (SearchDebounceTime.HasValue && bitNavPanel.HasNotBeenSet(nameof(SearchDebounceTime)))
        {
            bitNavPanel.SearchDebounceTime = SearchDebounceTime.Value;
        }

        if (SearchIcon is not null &&
            bitNavPanel.HasNotBeenSet(nameof(SearchIcon)) &&
            bitNavPanel.HasNotBeenSet(nameof(SearchIconName)))
        {
            bitNavPanel.SearchIcon = SearchIcon;
        }

        if (SearchIconName.HasValue() && bitNavPanel.HasNotBeenSet(nameof(SearchIconName)))
        {
            bitNavPanel.SearchIconName = SearchIconName;
        }

        if (ShowCloseButton.HasValue && bitNavPanel.HasNotBeenSet(nameof(ShowCloseButton)))
        {
            bitNavPanel.ShowCloseButton = ShowCloseButton.Value;
        }

        if (SingleExpand.HasValue && bitNavPanel.HasNotBeenSet(nameof(SingleExpand)))
        {
            bitNavPanel.SingleExpand = SingleExpand.Value;
        }

        if (Size.HasValue && bitNavPanel.HasNotBeenSet(nameof(Size)))
        {
            bitNavPanel.Size = Size.Value;
        }

        if (StickyEnds.HasValue && bitNavPanel.HasNotBeenSet(nameof(StickyEnds)) && bitNavPanel.StickyEnds != StickyEnds.Value)
        {
            bitNavPanel.StickyEnds = StickyEnds.Value;

            bitNavPanel.ClassBuilder.Reset();
        }

        if (Styles is not null && bitNavPanel.HasNotBeenSet(nameof(Styles)) && ReferenceEquals(bitNavPanel.Styles, Styles) is false)
        {
            bitNavPanel.Styles = Styles;

            bitNavPanel.StyleBuilder.Reset();
        }

        if (ToggleAriaLabel.HasValue() && bitNavPanel.HasNotBeenSet(nameof(ToggleAriaLabel)))
        {
            bitNavPanel.ToggleAriaLabel = ToggleAriaLabel;
        }

        if (ToggledWidth.HasValue && bitNavPanel.HasNotBeenSet(nameof(ToggledWidth)) && bitNavPanel.ToggledWidth != ToggledWidth.Value)
        {
            bitNavPanel.ToggledWidth = ToggledWidth.Value;

            bitNavPanel.StyleBuilder.Reset();
        }

        if (ToggleIcon is not null &&
            bitNavPanel.HasNotBeenSet(nameof(ToggleIcon)) &&
            bitNavPanel.HasNotBeenSet(nameof(ToggleIconName)))
        {
            bitNavPanel.ToggleIcon = ToggleIcon;
        }

        if (ToggleIconName.HasValue() && bitNavPanel.HasNotBeenSet(nameof(ToggleIconName)))
        {
            bitNavPanel.ToggleIconName = ToggleIconName;
        }

        if (Top.HasValue && bitNavPanel.HasNotBeenSet(nameof(Top)) && bitNavPanel.Top != Top.Value)
        {
            bitNavPanel.Top = Top.Value;

            bitNavPanel.StyleBuilder.Reset();
        }

        if (Width.HasValue && bitNavPanel.HasNotBeenSet(nameof(Width)) && bitNavPanel.Width != Width.Value)
        {
            bitNavPanel.Width = Width.Value;

            bitNavPanel.StyleBuilder.Reset();
        }
    }
}
