using Microsoft.AspNetCore.Components.Forms;

namespace Bit.BlazorUI;

/// <summary>
/// The ButtonGroup joins related buttons into a single unit: a plain action toolbar, or a single-select or
/// multi-select group of toggle buttons.
/// </summary>
/// <remarks>
/// The whole group is a single tab stop that the arrow, Home, and End keys navigate (a roving tabindex), and it
/// follows the WAI-ARIA pattern matching its <see cref="SelectionMode"/>: a radiogroup of radio buttons reporting
/// aria-checked in the Single mode, and a toolbar of toggle buttons reporting aria-pressed otherwise.
/// <br />
/// Give the group an accessible name through <see cref="BitComponentBase.AriaLabel"/>, since assistive technologies
/// do not announce an unlabeled group or toolbar.
/// </remarks>
public partial class BitButtonGroup<TItem> : BitComponentBase where TItem : class
{
    private int _optionKeySeed;
    private TItem? _toggleItem;
    private string? _focusedKey;
    private bool _preventKeyDownDefault;
    private List<TItem> _items = [];
    private string? _internalToggleKey;
    private List<TItem> _toggledItems = [];
    private IEnumerable<TItem> _oldItems = default!;
    private IEnumerable<string>? _internalToggleKeys;
    private readonly Dictionary<TItem, ElementReference> _itemElements = [];


    /// <summary>
    /// The EditContext, which is set if the button is inside an <see cref="EditForm"/>
    /// </summary>
    [CascadingParameter] private EditContext? _editContext { get; set; }



    /// <summary>
    /// The content of the BitButtonGroup, that are BitButtonGroupOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for different parts of the ButtonGroup.
    /// </summary>
    [Parameter] public BitButtonGroupClassStyles? Classes { get; set; }

    /// <summary>
    /// Defines the general colors available in the bit BlazorUI.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitColor? Color { get; set; }

    /// <summary>
    /// The default key that will be initially used to set toggled item in toggle mode if the ToggleKey parameter is not set.
    /// </summary>
    [Parameter] public string? DefaultToggleKey { get; set; }

    /// <summary>
    /// The default keys that will be initially used to set the toggled items in the Multiple selection mode
    /// if the ToggleKeys parameter is not set.
    /// </summary>
    [Parameter] public IEnumerable<string>? DefaultToggleKeys { get; set; }

    /// <summary>
    /// Detaches the buttons from each other, so each button is rendered as a separate rounded button.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Detached { get; set; }

    /// <summary>
    /// Keeps the disabled buttons focusable by rendering them with the aria-disabled attribute instead of
    /// the disabled attribute, so that assistive technologies can still discover them.
    /// </summary>
    [Parameter] public bool DisabledInteractive { get; set; }

    /// <summary>
    /// Enables the fixed-toggle mode that ensures one item to be always toggled.
    /// In the Multiple selection mode it prevents un-toggling the last toggled item.
    /// </summary>
    [Parameter] public bool FixedToggle { get; set; }

    /// <summary>
    /// Expand the ButtonGroup width to 100% of the available width.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool FullWidth { get; set; }

    /// <summary>
    /// The gap between the buttons of the ButtonGroup in the detached mode.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public string? Gap { get; set; }

    /// <summary>
    /// Determines that only the icon should be rendered.
    /// </summary>
    [Parameter] public bool IconOnly { get; set; }

    /// <summary>
    /// Gives every button an equal width so that the buttons evenly fill the width of the ButtonGroup.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Justified { get; set; }

    /// <summary>
    ///  List of Item, each of which can be a button with different action in the ButtonGroup.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// The content inside the item can be customized.
    /// </summary>
    [Parameter] public RenderFragment<TItem>? ItemTemplate { get; set; }

    /// <summary>
    /// The maximum number of items that can be toggled at the same time in the Multiple selection mode.
    /// </summary>
    [Parameter] public int? MaxToggles { get; set; }

    /// <summary>
    /// Names and selectors of the custom input type properties.
    /// </summary>
    [Parameter] public BitButtonGroupNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// Enables the roving tabindex behavior, which turns the whole ButtonGroup into a single tab stop
    /// that is navigable using the arrow, Home, and End keys.
    /// </summary>
    [Parameter] public bool Navigable { get; set; } = true;

    /// <summary>
    /// The callback that is called when a button is clicked.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// The callback that called when toggled item change.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnToggleChange { get; set; }

    /// <summary>
    /// Alias of ChildContent.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Determines how the ButtonGroup behaves when its buttons do not fit in the available space.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitButtonGroupOverflow? Overflow { get; set; }

    /// <summary>
    /// Renders the ButtonGroup with fully rounded (pill shaped) corners.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Rounded { get; set; }

    /// <summary>
    /// Toggles the focused item while navigating the ButtonGroup using the keyboard,
    /// so that the selection follows the focus.
    /// </summary>
    [Parameter] public bool SelectOnFocus { get; set; }

    /// <summary>
    /// Determines how many items can be toggled at the same time.
    /// When not set, it falls back to Single if the Toggle parameter is enabled, otherwise None.
    /// </summary>
    [Parameter] public BitButtonGroupSelectionMode? SelectionMode { get; set; }

    /// <summary>
    /// Renders a check mark at the start of the toggled buttons.
    /// </summary>
    [Parameter] public bool ShowSelectionIndicator { get; set; }

    /// <summary>
    /// The size of ButtonGroup, Possible values: Small | Medium | Large
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the ButtonGroup.
    /// </summary>
    [Parameter] public BitButtonGroupClassStyles? Styles { get; set; }

    /// <summary>
    /// Display ButtonGroup with toggle mode enabled for each button.
    /// It is a shorthand of setting the SelectionMode parameter to Single.
    /// </summary>
    [Parameter] public bool Toggle { get; set; }

    /// <summary>
    /// The key of the toggled item in the Single selection mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    public string? ToggleKey { get; set; }

    /// <summary>
    /// The keys of the toggled items in the Multiple selection mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    public IEnumerable<string>? ToggleKeys { get; set; }

    /// <summary>
    /// The visual variant of the button group.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitVariant? Variant { get; set; }

    /// <summary>
    /// Defines whether to render ButtonGroup children vertically.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    /// <summary>
    /// The effective selection mode, which falls back to the legacy Toggle parameter when SelectionMode is not set.
    /// </summary>
    internal BitButtonGroupSelectionMode _Mode => SelectionMode ?? (Toggle ? BitButtonGroupSelectionMode.Single : BitButtonGroupSelectionMode.None);



    internal void RegisterOption(BitButtonGroupOption option)
    {
        if (option.Key.HasNoValue())
        {
            // Use a monotonic seed so keys stay unique even after removals (a _items.Count-based key can
            // collide with an existing one once an option is removed), and guard against colliding with
            // any explicitly supplied keys.
            var key = (_optionKeySeed++).ToString();
            while (_items.Any(i => GetItemKey(i) == key))
            {
                key = (_optionKeySeed++).ToString();
            }
            option.Key = key;
        }

        var item = (option as TItem)!;

        _items.Add(item);

        if (_Mode is BitButtonGroupSelectionMode.Single)
        {
            var toggleKey = string.Empty;

            if (ToggleKeyHasBeenSet)
            {
                toggleKey = ToggleKey;
            }
            else if (DefaultToggleKey.HasValue())
            {
                toggleKey = DefaultToggleKey;
            }

            if (toggleKey.HasValue() && option.Key == toggleKey)
            {
                _ = UpdateItemToggle(item, false);
            }
        }
        else if (_Mode is BitButtonGroupSelectionMode.Multiple)
        {
            var toggleKeys = ToggleKeysHasBeenSet ? ToggleKeys : DefaultToggleKeys;

            if (toggleKeys is not null && option.Key.HasValue() && toggleKeys.Contains(option.Key!))
            {
                _toggledItems.Add(item);
                SetIsToggled(item, true);
            }
        }

        StateHasChanged();
    }

    internal void UnregisterOption(BitButtonGroupOption option)
    {
        var item = (option as TItem)!;

        // When the removed option is the currently toggled one, clear the toggle state and the bound
        // key so they don't keep referencing an option that no longer exists.
        if (_toggleItem == item)
        {
            _toggleItem = null;
            _ = AssignToggleKey(null);
        }

        if (_toggledItems.Remove(item))
        {
            _ = AssignToggleKeys(GetToggledKeys());
        }

        _itemElements.Remove(item);
        _items.Remove(item);
        StateHasChanged();
    }

    internal void RegisterItemElement(TItem item, ElementReference element)
    {
        _itemElements[item] = element;
    }



    protected override string RootElementClass => "bit-btg";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Variant switch
        {
            BitVariant.Fill => "bit-btg-fil",
            BitVariant.Outline => "bit-btg-otl",
            BitVariant.Text => "bit-btg-txt",
            _ => "bit-btg-fil"
        });

        ClassBuilder.Register(() => Color switch
        {
            BitColor.Primary => "bit-btg-pri",
            BitColor.Secondary => "bit-btg-sec",
            BitColor.Tertiary => "bit-btg-ter",
            BitColor.Info => "bit-btg-inf",
            BitColor.Success => "bit-btg-suc",
            BitColor.Warning => "bit-btg-wrn",
            BitColor.SevereWarning => "bit-btg-swr",
            BitColor.Error => "bit-btg-err",
            BitColor.PrimaryBackground => "bit-btg-pbg",
            BitColor.SecondaryBackground => "bit-btg-sbg",
            BitColor.TertiaryBackground => "bit-btg-tbg",
            BitColor.PrimaryForeground => "bit-btg-pfg",
            BitColor.SecondaryForeground => "bit-btg-sfg",
            BitColor.TertiaryForeground => "bit-btg-tfg",
            BitColor.PrimaryBorder => "bit-btg-pbr",
            BitColor.SecondaryBorder => "bit-btg-sbr",
            BitColor.TertiaryBorder => "bit-btg-tbr",
            _ => "bit-btg-pri"
        });

        ClassBuilder.Register(() => Size switch
        {
            BitSize.Small => "bit-btg-sm",
            BitSize.Medium => "bit-btg-md",
            BitSize.Large => "bit-btg-lg",
            _ => "bit-btg-md"
        });

        ClassBuilder.Register(() => Vertical ? "bit-btg-vrt" : string.Empty);

        ClassBuilder.Register(() => FullWidth ? "bit-btg-flw" : string.Empty);

        ClassBuilder.Register(() => Justified ? "bit-btg-jst" : string.Empty);

        ClassBuilder.Register(() => Rounded ? "bit-btg-rnd" : string.Empty);

        ClassBuilder.Register(() => Detached ? "bit-btg-dtc" : string.Empty);

        ClassBuilder.Register(() => Overflow switch
        {
            BitButtonGroupOverflow.Wrap => "bit-btg-wrp",
            BitButtonGroupOverflow.Scroll => "bit-btg-scr",
            BitButtonGroupOverflow.Scrollbar => "bit-btg-scb",
            _ => string.Empty
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);

        StyleBuilder.Register(() => Gap.HasValue() ? $"--bit-btg-gap:{Gap}" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        // Only seed _items from Items for the Items API; in the options/child-content path the options
        // register themselves, so it must start empty.
        _items = (ChildContent is null && Options is null && Items is not null) ? [.. Items] : [];

        if (Items is not null && Items.Any())
        {
            if (_Mode is BitButtonGroupSelectionMode.Single)
            {
                var toggleKey = string.Empty;

                if (ToggleKeyHasBeenSet)
                {
                    toggleKey = ToggleKey;
                    _internalToggleKey = ToggleKey;
                }
                else if (DefaultToggleKey.HasValue())
                {
                    toggleKey = DefaultToggleKey;
                }

                if (toggleKey.HasValue())
                {
                    var item = Items.FirstOrDefault(i => GetItemKey(i) == toggleKey);
                    await UpdateItemToggle(item, false);
                }
            }
            else if (_Mode is BitButtonGroupSelectionMode.Multiple)
            {
                IEnumerable<string>? toggleKeys = null;

                if (ToggleKeysHasBeenSet)
                {
                    toggleKeys = ToggleKeys;
                    _internalToggleKeys = ToggleKeys;
                }
                else if (DefaultToggleKeys is not null)
                {
                    toggleKeys = DefaultToggleKeys;
                }

                if (toggleKeys is not null)
                {
                    ApplyToggleKeys(toggleKeys);
                }
            }
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ChildContent is null && Options is null && Items is not null && Items.Any())
        {
            if (_oldItems is null || (ReferenceEquals(Items, _oldItems) is false && Items.SequenceEqual(_oldItems) is false))
            {
                _oldItems = Items;
                _items = [.. Items];

                AssignItemKeys();
            }
        }

        if (_internalToggleKey != ToggleKey)
        {
            _internalToggleKey = ToggleKey;

            if (_internalToggleKey.HasValue())
            {
                var item = _items.FirstOrDefault(i => GetItemKey(i) == _internalToggleKey);
                await UpdateItemToggle(item, false);
            }
        }

        if (_Mode is BitButtonGroupSelectionMode.Multiple &&
            (_internalToggleKeys is null
                ? ToggleKeys is not null
                : ToggleKeys is null || _internalToggleKeys.SequenceEqual(ToggleKeys) is false))
        {
            _internalToggleKeys = ToggleKeys;

            ApplyToggleKeys(ToggleKeys ?? []);
        }

        // Options render their items themselves and Blazor skips re-rendering them when only the
        // button group's own parameters (Styles, Toggle, IconOnly, ...) change, so push a re-render to each one.
        RefreshOptions();

        await base.OnParametersSetAsync();
    }



    private void RefreshOptions()
    {
        // In the Items API there are no registered options, so there is nothing to refresh.
        if ((Options ?? ChildContent) is null) return;

        foreach (var item in _items)
        {
            (item as BitButtonGroupOption)?.InternalStateHasChanged();
        }
    }

    private void AssignItemKeys()
    {
        // Collect the explicit keys first so the auto-generated keys never collide with them.
        var usedKeys = new HashSet<string>();
        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            if (key.HasValue()) usedKeys.Add(key!);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (GetItemKey(item).HasValue()) continue;

            // Start from the loop index and increment until a non-colliding key is found so the
            // result stays deterministic across renders while remaining unique.
            var suffix = i;
            var candidate = suffix.ToString();
            while (usedKeys.Contains(candidate))
            {
                candidate = (++suffix).ToString();
            }

            SetItemKey(item, candidate);
            usedKeys.Add(candidate);
        }
    }



    internal async Task HandleOnItemClick(TItem item)
    {
        if (GetIsEnabled(item) is false) return;
        if (GetIsLoading(item)) return;

        _focusedKey = GetItemKey(item);

        await OnItemClick.InvokeAsync(item);

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            buttonGroupItem.OnClick?.Invoke(buttonGroupItem);
        }
        else if (item is BitButtonGroupOption buttonGroupOption)
        {
            await buttonGroupOption.OnClick.InvokeAsync(buttonGroupOption);
        }
        else if (NameSelectors is not null)
        {
            if (NameSelectors.OnClick.Selector is not null)
            {
                NameSelectors.OnClick.Selector!(item)?.Invoke(item);
            }
            else
            {
                item.GetValueFromProperty<Action<TItem>?>(NameSelectors.OnClick.Name)?.Invoke(item);
            }
        }

        await UpdateItemToggle(item);
    }

    // The whole group is a single tab stop: the toggled item (or the first focusable one) holds the
    // tabindex and the arrow keys move the focus between the items, as described by the WAI-ARIA
    // radiogroup and toolbar patterns.
    internal async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        // ArrowUp, ArrowDown, Home and End scroll the page by default, so their default action is
        // suppressed while the group navigates with them. Kept key-scoped so Space, Enter, Tab and
        // the horizontal arrows outside of a vertical group still behave normally.
        _preventKeyDownDefault = Navigable && IsEnabled && e.Key is "ArrowUp" or "ArrowDown" or "Home" or "End";

        if (Navigable is false) return;
        if (IsEnabled is false) return;

        var focusables = _items.Where(IsItemFocusable).ToList();
        if (focusables.Count == 0) return;

        var current = GetActiveItem();
        var index = current is null ? -1 : focusables.IndexOf(current);
        var isRtl = (Dir ?? CascadingDir) == BitDir.Rtl;

        int next;
        switch (e.Key)
        {
            case "ArrowRight":
                if (Vertical) return;
                next = isRtl ? index - 1 : index + 1;
                break;
            case "ArrowLeft":
                if (Vertical) return;
                next = isRtl ? index + 1 : index - 1;
                break;
            case "ArrowDown":
                if (Vertical is false) return;
                next = index + 1;
                break;
            case "ArrowUp":
                if (Vertical is false) return;
                next = index - 1;
                break;
            case "Home":
                next = 0;
                break;
            case "End":
                next = focusables.Count - 1;
                break;
            default:
                return;
        }

        // The navigation wraps around at both ends of the group.
        if (next < 0) next = focusables.Count - 1;
        else if (next >= focusables.Count) next = 0;

        var item = focusables[next];

        _focusedKey = GetItemKey(item);

        if (_itemElements.TryGetValue(item, out var element))
        {
            try
            {
                await element.FocusAsync();
            }
            catch (JSDisconnectedException) { } // we can ignore this exception here
        }

        if (SelectOnFocus && GetIsEnabled(item) && GetIsLoading(item) is false)
        {
            await UpdateItemToggle(item);
        }

        RefreshOptions();
        StateHasChanged();
    }

    internal string? GetItemTabIndex(TItem item)
    {
        if (Navigable is false) return IsItemFocusable(item) ? "0" : "-1";

        return GetActiveItem() == item ? "0" : "-1";
    }

    /// <summary>
    /// The role of an individual button, which follows the selection mode of the group.
    /// </summary>
    internal string? GetItemRole()
    {
        return _Mode is BitButtonGroupSelectionMode.Single ? "radio" : null;
    }

    /// <summary>
    /// In the Single selection mode the toggle state is announced by aria-checked (radio semantics),
    /// so aria-pressed must not be rendered as well.
    /// </summary>
    internal string? GetItemAriaPressed(TItem item)
    {
        if (_Mode is not BitButtonGroupSelectionMode.Multiple) return null;

        return IsItemToggled(item) ? "true" : "false";
    }

    internal string? GetItemAriaChecked(TItem item)
    {
        if (_Mode is not BitButtonGroupSelectionMode.Single) return null;

        return IsItemToggled(item) ? "true" : "false";
    }

    /// <summary>
    /// The role of the root element, which follows the WAI-ARIA pattern matching the selection mode.
    /// </summary>
    internal string _RootRole => _Mode switch
    {
        BitButtonGroupSelectionMode.Single => "radiogroup",
        BitButtonGroupSelectionMode.Multiple => Navigable ? "toolbar" : "group",
        _ => Navigable ? "toolbar" : "group"
    };

    /// <summary>
    /// The aria-orientation is only supported by the radiogroup and toolbar roles, not by the plain group role.
    /// </summary>
    internal string? _AriaOrientation => _RootRole is "group" ? null : (Vertical ? "vertical" : "horizontal");

    // The item that owns the group's tabindex: the last focused one, otherwise the toggled one,
    // otherwise the first focusable item.
    private TItem? GetActiveItem()
    {
        var focusables = _items.Where(IsItemFocusable).ToList();
        if (focusables.Count == 0) return null;

        if (_focusedKey.HasValue())
        {
            var focused = focusables.FirstOrDefault(i => GetItemKey(i) == _focusedKey);
            if (focused is not null) return focused;
        }

        var toggled = focusables.FirstOrDefault(IsItemToggled);
        if (toggled is not null) return toggled;

        return focusables[0];
    }

    // Disabled items stay focusable in the DisabledInteractive mode, which the WAI-ARIA toolbar
    // pattern recommends so that they remain discoverable by assistive technologies.
    private bool IsItemFocusable(TItem item)
    {
        return DisabledInteractive || GetIsEnabled(item);
    }

    internal string? GetItemClass(TItem item)
    {
        List<string> classes = ["bit-btg-itm"];

        // The first/last buttons are marked explicitly instead of relying on :first-child/:last-child,
        // which break as soon as a button is wrapped by another element.
        var index = _items.IndexOf(item);
        if (index == 0)
        {
            classes.Add("bit-btg-fst");
        }
        if (index == _items.Count - 1)
        {
            classes.Add("bit-btg-lst");
        }

        if (GetReversedIcon(item))
        {
            classes.Add("bit-btg-rvi");
        }

        if (GetIsLoading(item))
        {
            classes.Add("bit-btg-ldg");
        }

        if (IsItemToggled(item))
        {
            classes.Add("bit-btg-chk");

            if (Classes?.ToggledButton.HasValue() ?? false)
            {
                classes.Add(Classes.ToggledButton!);
            }
        }

        var classItem = GetClass(item);
        if (classItem.HasValue())
        {
            classes.Add(classItem!);
        }

        if (Classes?.Button.HasValue() ?? false)
        {
            classes.Add(Classes.Button!);
        }

        return string.Join(' ', classes);
    }

    internal string? GetItemStyle(TItem item)
    {
        List<string> styles = [];

        var style = GetStyle(item);
        if (style.HasValue())
        {
            styles.Add(style!.Trim(';'));
        }

        if (Styles?.Button.HasValue() ?? false)
        {
            styles.Add(Styles.Button!.Trim(';'));
        }

        if (IsItemToggled(item) && (Styles?.ToggledButton.HasValue() ?? false))
        {
            styles.Add(Styles.ToggledButton!);
        }

        return string.Join(';', styles);
    }

    internal string? GetItemText(TItem item)
    {
        if (IconOnly) return null;

        if (_Mode is not BitButtonGroupSelectionMode.None)
        {
            if (IsItemToggled(item))
            {
                var onText = GetOnText(item);
                if (onText.HasValue())
                {
                    return onText;
                }
            }
            else
            {
                var offText = GetOffText(item);
                if (offText.HasValue())
                {
                    return offText;
                }
            }
        }

        return GetText(item);
    }

    internal string? GetItemTitle(TItem item)
    {
        if (_Mode is not BitButtonGroupSelectionMode.None)
        {
            if (IsItemToggled(item))
            {
                var onTitle = GetOnTitle(item);
                if (onTitle.HasValue())
                {
                    return onTitle;
                }
            }
            else
            {
                var offTitle = GetOffTitle(item);
                if (offTitle.HasValue())
                {
                    return offTitle;
                }
            }
        }

        return GetTitle(item);
    }

    internal BitIconInfo? GetItemIcon(TItem item)
    {
        if (_Mode is not BitButtonGroupSelectionMode.None)
        {
            if (IsItemToggled(item))
            {
                var onIcon = GetOnIcon(item);
                if (onIcon is not null) return onIcon;

                var onIconName = GetOnIconName(item);
                if (onIconName.HasValue()) return new BitIconInfo(onIconName!);
            }
            else
            {
                var offIcon = GetOffIcon(item);
                if (offIcon is not null) return offIcon;

                var offIconName = GetOffIconName(item);
                if (offIconName.HasValue()) return new BitIconInfo(offIconName!);
            }
        }

        return BitIconInfo.From(GetIcon(item), GetIconName(item));
    }

    private async Task UpdateItemToggle(TItem? item, bool isToggled = true)
    {
        if (item is null) return;
        if (_items is null || _items.Count == 0) return;

        if (_Mode is BitButtonGroupSelectionMode.Multiple)
        {
            await UpdateItemToggleMultiple(item);
            return;
        }

        if (_Mode is not BitButtonGroupSelectionMode.Single) return;
        if (ToggleKeyHasBeenSet && ToggleKeyChanged.HasDelegate is false) return;

        string? toggleKey = GetItemKey(_toggleItem);
        var oldToggledItem = _items.FirstOrDefault(IsItemToggled);

        if (oldToggledItem == item && (isToggled is false || FixedToggle)) return;

        if (oldToggledItem != item)
        {
            _toggleItem = item;
            SetIsToggled(item, true);
            toggleKey = GetItemKey(item);
        }
        else
        {
            toggleKey = null;
            _toggleItem = null;
        }

        if (oldToggledItem is not null)
        {
            SetIsToggled(oldToggledItem, false);
        }

        await AssignToggleKey(toggleKey);
        await OnToggleChange.InvokeAsync(item);

        // A toggle change affects the rendering of the previously and newly toggled items, but the click
        // handler now runs on the clicked item's renderer, so both the parent (Items API) and the
        // registered options need an explicit re-render.
        RefreshOptions();
        StateHasChanged();
    }

    private async Task UpdateItemToggleMultiple(TItem item)
    {
        if (ToggleKeysHasBeenSet && ToggleKeysChanged.HasDelegate is false) return;

        if (_toggledItems.Contains(item))
        {
            // FixedToggle keeps at least one item toggled, so the last one cannot be un-toggled.
            if (FixedToggle && _toggledItems.Count == 1) return;

            _toggledItems.Remove(item);
            SetIsToggled(item, false);
        }
        else
        {
            if (MaxToggles is int max && max > 0 && _toggledItems.Count >= max) return;

            _toggledItems.Add(item);
            SetIsToggled(item, true);
        }

        var keys = GetToggledKeys();
        _internalToggleKeys = keys;
        await AssignToggleKeys(keys);
        await OnToggleChange.InvokeAsync(item);

        RefreshOptions();
        StateHasChanged();
    }

    // Rebuilds the toggled items from the given keys without raising the change callbacks,
    // which is what both the initial toggle keys and the two-way bound updates need.
    private void ApplyToggleKeys(IEnumerable<string> keys)
    {
        var keySet = keys.ToHashSet();

        foreach (var item in _toggledItems)
        {
            SetIsToggled(item, false);
        }

        _toggledItems = [];

        foreach (var item in _items)
        {
            var key = GetItemKey(item);
            if (key.HasNoValue() || keySet.Contains(key!) is false) continue;
            if (MaxToggles is int max && max > 0 && _toggledItems.Count >= max) break;

            _toggledItems.Add(item);
            SetIsToggled(item, true);
        }
    }

    // The keys always follow the order of the items, not the order they were toggled in.
    private List<string> GetToggledKeys()
    {
        return [.. _items.Where(_toggledItems.Contains)
                         .Select(GetItemKey)
                         .Where(k => k.HasValue())
                         .Select(k => k!)];
    }

    private bool IsItemToggled(TItem item)
    {
        return _Mode is BitButtonGroupSelectionMode.Multiple
                ? _toggledItems.Contains(item)
                : _toggleItem == item;
    }

    private void SetIsToggled(TItem item, bool value)
    {
        if (item is BitButtonGroupItem buttonGroupItem)
        {
            buttonGroupItem.IsToggled = value;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            buttonGroupOption.IsToggled = value;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.IsToggled.Name, value);
    }

    private string? GetItemKey(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Key;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Key;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Key.Selector is not null)
        {
            return NameSelectors.Key.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Key.Name);
    }

    private void SetItemKey(TItem item, string value)
    {
        if (item is BitButtonGroupItem buttonGroupItem)
        {
            buttonGroupItem.Key = value;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            buttonGroupOption.Key = value;
        }

        if (NameSelectors is null) return;

        item.SetValueToProperty(NameSelectors.Key.Name, value);
    }

    private string? GetClass(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Class;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Class;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Class.Selector is not null)
        {
            return NameSelectors.Class.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Class.Name);
    }

    private BitIconInfo? GetIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Icon;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Icon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Icon.Selector is not null)
        {
            return NameSelectors.Icon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.Icon.Name);
    }

    private string? GetIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.IconName;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.IconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.IconName.Selector is not null)
        {
            return NameSelectors.IconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.IconName.Name);
    }

    private BitIconInfo? GetOnIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OnIcon;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OnIcon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OnIcon.Selector is not null)
        {
            return NameSelectors.OnIcon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.OnIcon.Name);
    }

    private string? GetOnIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OnIconName;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OnIconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OnIconName.Selector is not null)
        {
            return NameSelectors.OnIconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OnIconName.Name);
    }

    private BitIconInfo? GetOffIcon(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OffIcon;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OffIcon;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OffIcon.Selector is not null)
        {
            return NameSelectors.OffIcon.Selector!(item);
        }

        return item.GetValueFromProperty<BitIconInfo?>(NameSelectors.OffIcon.Name);
    }

    private string? GetOffIconName(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OffIconName;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OffIconName;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OffIconName.Selector is not null)
        {
            return NameSelectors.OffIconName.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OffIconName.Name);
    }

    internal string? GetAriaLabel(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.AriaLabel;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.AriaLabel;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.AriaLabel.Selector is not null)
        {
            return NameSelectors.AriaLabel.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.AriaLabel.Name);
    }

    internal string? GetBadge(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Badge;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Badge;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Badge.Selector is not null)
        {
            return NameSelectors.Badge.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Badge.Name);
    }

    internal string? GetHref(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Href;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Href;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Href.Selector is not null)
        {
            return NameSelectors.Href.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Href.Name);
    }

    internal string? GetTarget(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Target;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Target;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Target.Selector is not null)
        {
            return NameSelectors.Target.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Target.Name);
    }

    internal bool GetIsLoading(TItem? item)
    {
        if (item is null) return false;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.IsLoading;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.IsLoading;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.IsLoading.Selector is not null)
        {
            return NameSelectors.IsLoading.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsLoading.Name, false);
    }

    internal bool GetIsEnabled(TItem? item)
    {
        if (item is null) return false;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.IsEnabled;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.IsEnabled;
        }

        if (NameSelectors is null) return true;

        if (NameSelectors.IsEnabled.Selector is not null)
        {
            return NameSelectors.IsEnabled.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.IsEnabled.Name, true);
    }

    private string? GetStyle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Style;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Style;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Style.Selector is not null)
        {
            return NameSelectors.Style.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Style.Name);
    }

    internal RenderFragment<TItem>? GetTemplate(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Template as RenderFragment<TItem>;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Template as RenderFragment<TItem>;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Template.Selector is not null)
        {
            return NameSelectors.Template.Selector!(item);
        }

        return item.GetValueFromProperty<RenderFragment<TItem>?>(NameSelectors.Template.Name);
    }

    private string? GetText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Text;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Text;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Text.Selector is not null)
        {
            return NameSelectors.Text.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Text.Name);
    }

    private string? GetOnText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OnText;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OnText;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OnText.Selector is not null)
        {
            return NameSelectors.OnText.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OnText.Name);
    }

    private string? GetOffText(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OffText;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OffText;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OffText.Selector is not null)
        {
            return NameSelectors.OffText.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OffText.Name);
    }

    private string? GetTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.Title;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.Title;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.Title.Selector is not null)
        {
            return NameSelectors.Title.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.Title.Name);
    }

    private string? GetOnTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OnTitle;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OnTitle;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OnTitle.Selector is not null)
        {
            return NameSelectors.OnTitle.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OnTitle.Name);
    }

    private string? GetOffTitle(TItem? item)
    {
        if (item is null) return null;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.OffTitle;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.OffTitle;
        }

        if (NameSelectors is null) return null;

        if (NameSelectors.OffTitle.Selector is not null)
        {
            return NameSelectors.OffTitle.Selector!(item);
        }

        return item.GetValueFromProperty<string?>(NameSelectors.OffTitle.Name);
    }

    private bool GetReversedIcon(TItem? item)
    {
        if (item is null) return false;

        if (item is BitButtonGroupItem buttonGroupItem)
        {
            return buttonGroupItem.ReversedIcon;
        }

        if (item is BitButtonGroupOption buttonGroupOption)
        {
            return buttonGroupOption.ReversedIcon;
        }

        if (NameSelectors is null) return false;

        if (NameSelectors.ReversedIcon.Selector is not null)
        {
            return NameSelectors.ReversedIcon.Selector!(item);
        }

        return item.GetValueFromProperty(NameSelectors.ReversedIcon.Name, false);
    }
}
