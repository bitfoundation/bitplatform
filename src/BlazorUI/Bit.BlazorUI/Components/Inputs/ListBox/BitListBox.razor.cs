using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Bit.BlazorUI;

/// <summary>
/// BitListBox is a list the user picks one item from, or several, that is always on the page rather than in a popup:
/// a form input like BitDropdown, without the dropdown.
/// </summary>
/// <remarks>
/// It is a WAI-ARIA listbox: a single tab stop, from which the arrow keys, Home, End and typing the start of a text
/// move between the items. In single select mode the selection follows the keyboard, the way a native list does; with
/// <see cref="MultiSelect"/>, Space (or Enter, or a click) adds the item to the selection or takes it out, and the
/// selection is <see cref="Values"/>. Both bind to an EditForm and are validated with it.
/// <br />
/// The items are given in one of three ways: <see cref="BitListBoxItem{TValue}"/> objects in <see cref="Items"/>,
/// objects of a custom class in <see cref="Items"/> read through <see cref="NameSelectors"/>, or
/// <see cref="BitListBoxOption{TValue}"/> components in <see cref="Options"/> (or as the child content).
/// </remarks>
public partial class BitListBox<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem, TValue> : BitInputBase<TValue> where TItem : class
{
    // The keys the list box answers itself, whose default action (scrolling the page) is kept from happening.
    private static readonly string[] _navigationKeys = ["ArrowUp", "ArrowDown", "Home", "End", " "];

    // How long a pause in typing starts a new typeahead search.
    private static readonly TimeSpan _typeaheadTimeout = TimeSpan.FromMilliseconds(1000);

    // Why the options of TItem never reach a list box of TValue, or null when they do (see OnInitialized).
    private static readonly string? _optionValueTypeMismatch = BitItemValueType.GetOptionMismatch(nameof(BitListBox<TItem, TValue>), typeof(TItem), typeof(TValue), typeof(BitListBoxOption<>));



    private List<TItem> _items = [];
    private IEnumerable<TItem>? _oldItems;
    // By the key each item is rendered with, rather than by position: Blazor hands an element its reference only when it
    // creates it, so the reference of an item kept across a render (moved by a reorder, or by new Items) is the one it
    // was created with, and the collection is never replaced.
    private readonly Dictionary<string, ElementReference> _optionRefs = [];

    // Set when an option registers or goes away, so the order of the items is read back from the document after the
    // render (see ReorderOptions).
    private bool _optionsOrderDirty;
    private string _optionsContainerId = default!;

    // The item the keyboard is on: the one tab stop of the list, and the one moved by the keys.
    private int _activeIndex = -1;
    private bool _focusActiveAfterRender;
    private bool _keysRegistered;

    // Whether the single select Value names a selection at all. A Value never set is the default of its type, which
    // could be the value of an item (a 0 among int values) without that item ever having been chosen.
    private bool _hasValue;

    private string _typeahead = string.Empty;
    private DateTime _typeaheadAt;

    private string _listId = default!;
    private string _labelId = default!;
    private string _errorId = default!;

    private ElementReference _listRef;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The default values of the parameters of the list box, cascaded through a <see cref="BitParams"/>. A value set on
    /// the list box itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitListBoxParams.ParamName)]
    public BitListBoxParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content of the list box, that are BitListBoxOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the list box.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitListBoxClassStyles? Classes { get; set; }

    /// <summary>
    /// The values selected at first in multi select mode, when <see cref="Values"/> is not bound.
    /// </summary>
    [Parameter] public IEnumerable<TValue?>? DefaultValues { get; set; }

    /// <summary>
    /// A message saying what is wrong with the selection, shown under the list. Setting it marks the list box invalid.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Marks the list box invalid, with the same look and the same aria-invalid an <see cref="ErrorMessage"/> gives it.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Invalid { get; set; }

    /// <summary>
    /// The items of the list box.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// The visible label of the list box, which is also what names it to a screen reader.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The height (any CSS length) past which the list scrolls.
    /// </summary>
    [Parameter] public string? MaxHeight { get; set; }

    /// <summary>
    /// The names (or selectors) of the members the items are read from, for items of a custom class.
    /// </summary>
    [Parameter] public BitListBoxNameSelectors<TItem, TValue>? NameSelectors { get; set; }

    /// <summary>
    /// Lets the user select several items, which are <see cref="Values"/> instead of <see cref="BitInputBase{TValue}.Value"/>.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool MultiSelect { get; set; }

    /// <summary>
    /// The callback for when an item is taken out of the selection in multi select mode.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnDeselectItem { get; set; }

    /// <summary>
    /// The callback for when an item is selected (in multi select mode, added to the selection).
    /// </summary>
    [Parameter] public EventCallback<TItem> OnSelectItem { get; set; }

    /// <summary>
    /// Alias for the ChildContent parameter: the items declared as BitListBoxOption components.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Shows the validation messages of the cascading EditContext for the list box under it, where
    /// <see cref="ErrorMessage"/> goes, while there is no ErrorMessage of the app's own.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ShowValidationMessage { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the list box.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitListBoxClassStyles? Styles { get; set; }

    /// <summary>
    /// The values of the selected items in multi select mode. (two-way bound)
    /// </summary>
    [Parameter, TwoWayBound]
    [CallOnSet(nameof(OnSetValues))]
    public IEnumerable<TValue?>? Values { get; set; }

    /// <summary>
    /// The expression that identifies the bound <see cref="Values"/>, which is what ties the multi select mode to an
    /// EditContext. It is supplied by the compiler for a <c>@bind-Values</c> binding.
    /// </summary>
    [Parameter] public Expression<Func<IEnumerable<TValue?>?>>? ValuesExpression { get; set; }



    /// <summary>
    /// Moves the focus to the list box: to the item the keyboard is on, which is the selected one when it comes in.
    /// </summary>
    public override ValueTask FocusAsync() => FocusAsync(false);

    /// <summary>
    /// Moves the focus to the list box: to the item the keyboard is on, which is the selected one when it comes in.
    /// </summary>
    public override ValueTask FocusAsync(bool preventScroll)
    {
        var index = TabStopIndex;

        return FocusItemAsync(index, preventScroll);
    }



    protected override string RootElementClass => "bit-lbx";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => MultiSelect ? "bit-lbx-mul" : string.Empty);

        ClassBuilder.Register(() => ReadOnly ? "bit-lbx-rdo" : string.Empty);

        ClassBuilder.Register(() => Required ? "bit-lbx-req" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        // A list box of options that never reach it is refused before it renders an empty list.
        if (_optionValueTypeMismatch is not null)
        {
            throw new InvalidOperationException(_optionValueTypeMismatch);
        }

        _optionsContainerId = $"BitListBox-{UniqueId}-options";
        _listId = $"BitListBox-{UniqueId}-list";
        _labelId = $"BitListBox-{UniqueId}-label";
        _errorId = $"BitListBox-{UniqueId}-error";

        base.OnInitialized();

        if (MultiSelect)
        {
            if (ValuesHasBeenSet is false && DefaultValues is not null)
            {
                _ = AssignValues(DefaultValues);
            }
        }
        else
        {
            SetDefaultValue();
        }

        _hasValue = ValueHasBeenSet || DefaultValueHasBeenSet;
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // With options, the items are the options that registered themselves; Items is not read at all.
        if (ChildContent is null && Options is null && ReferenceEquals(_oldItems, Items) is false)
        {
            _oldItems = Items;
            SetItems(Items?.ToList() ?? []);
        }

        if (ValueHasBeenSet) _hasValue = true;

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_keysRegistered is false)
        {
            _keysRegistered = true;

            await _js.BitUtilsRegisterPreventKeys(_listRef, _navigationKeys);
        }

        if (_focusActiveAfterRender)
        {
            _focusActiveAfterRender = false;

            var index = TabStopIndex;
            await FocusItemAsync(index, false);
        }

        if (_optionsOrderDirty)
        {
            _optionsOrderDirty = false;

            try
            {
                var orderedOptionIds = await _js.BitUtilsGetChildrenAttributes(_optionsContainerId, BitListBoxOption<TValue>._OPTION_ID_ATTRIBUTE);

                if (IsDisposed || orderedOptionIds is null) return;

                ReorderOptions(orderedOptionIds);
            }
            catch (JSDisconnectedException) { } // the circuit is gone, so there is no page left to order the items for
            catch (JSException) { } // reading the markers failed, which only leaves the items in the order they registered
        }
    }



    internal void RegisterOption(BitListBoxOption<TValue> option)
    {
        SetItems([.. _items, (option as TItem)!]);
        _optionsOrderDirty = true;

        StateHasChanged();
    }

    internal void UnregisterOption(BitListBoxOption<TValue> option)
    {
        // The options unregister while they are disposed, which also happens when the list box itself goes away, so a
        // render is only asked for while the list box is still alive.
        if (IsDisposing) return;

        SetItems(_items.Where(i => ReferenceEquals(i, option) is false).ToList());
        _optionsOrderDirty = true;

        StateHasChanged();
    }

    // The list box draws the items from the options and reads their values while it renders, so a change to the
    // parameters of an option needs a render of the list box to show.
    internal void NotifyOptionParametersChanged()
    {
        if (IsDisposed) return;

        StateHasChanged();
    }

    // An option rendered after the others (an @if that turns true) registers last, wherever it sits in the markup. So
    // the items are put back in the order of the options' markers in the document, which is the order of the markup.
    internal void ReorderOptions(string[] orderedOptionIds)
    {
        if (orderedOptionIds.Length == 0) return;

        List<TItem> ordered = new(_items.Count);

        foreach (var optionId in orderedOptionIds)
        {
            var item = _items.FirstOrDefault(i => (i as BitListBoxOption<TValue>)?._OptionId == optionId);
            if (item is null || ordered.Contains(item)) continue;

            ordered.Add(item);
        }

        ordered.AddRange(_items.Except(ordered));

        if (ordered.SequenceEqual(_items)) return;

        // The keyboard stays on the item it was on, wherever that item has moved to.
        var active = _activeIndex >= 0 && _activeIndex < _items.Count ? _items[_activeIndex] : null;

        SetItems(ordered);

        if (active is not null) _activeIndex = _items.IndexOf(active);

        StateHasChanged();
    }

    private void SetItems(List<TItem> items)
    {
        _items = items;

        // The references of the items no longer in the list go with them.
        var keys = _items.Select((item, index) => GetItemKey(item, index)).ToHashSet();
        foreach (var key in _optionRefs.Keys.Where(k => keys.Contains(k) is false).ToList())
        {
            _optionRefs.Remove(key);
        }

        if (_activeIndex >= _items.Count) _activeIndex = -1;
    }

    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out TValue result, [NotNullWhen(false)] out string? parsingErrorMessage)
        => throw new NotSupportedException($"This component does not parse string inputs. Bind to the '{nameof(CurrentValue)}' property, not '{nameof(CurrentValueAsString)}'.");

    protected override void CreateFieldIdentifier()
    {
        if (MultiSelect)
        {
            CreateFieldIdentifier(ValuesExpression, typeof(ICollection<TValue?>));
        }
        else
        {
            base.CreateFieldIdentifier();
        }
    }

    /// <summary>
    /// Whether the app itself has rejected the selection, by the flag or by a message saying why.
    /// </summary>
    protected override bool HasError => Invalid || ErrorMessage.HasValue();



    // The message under the list: the app's own, or else the EditContext's when it is asked for.
    private string? ShownErrorMessage => ErrorMessage.HasValue() ? ErrorMessage
                                         : ShowValidationMessage ? FieldValidationMessage
                                         : null;

    // The one item of the list in the tab order: the one the keyboard is on, else the first selected one, else the
    // first that can be selected.
    private int TabStopIndex
    {
        get
        {
            if (_activeIndex >= 0 && _activeIndex < _items.Count && GetIsDisabled(_items[_activeIndex]) is false) return _activeIndex;

            var selected = _items.FindIndex(i => GetIsDisabled(i) is false && IsSelected(i));
            if (selected >= 0) return selected;

            return _items.FindIndex(i => GetIsDisabled(i) is false);
        }
    }

    private bool IsSelected(TItem item)
    {
        var value = GetValue(item);

        if (MultiSelect)
        {
            return Values?.Any(v => EqualityComparer<TValue>.Default.Equals(v, value)) ?? false;
        }

        return _hasValue && EqualityComparer<TValue>.Default.Equals(Value, value);
    }

    private async Task SelectAsync(TItem item)
    {
        if (Disabled || ReadOnly || GetIsDisabled(item)) return;

        var value = GetValue(item);

        if (MultiSelect)
        {
            var values = Values?.ToList() ?? [];
            var index = values.FindIndex(v => EqualityComparer<TValue>.Default.Equals(v, value));
            var selecting = index < 0;

            if (selecting)
            {
                values.Add(value);
            }
            else
            {
                values.RemoveAt(index);
            }

            if (await AssignValues(values) is false) return;

            await (selecting ? OnSelectItem : OnDeselectItem).InvokeAsync(item);
        }
        else
        {
            if (InvalidValueBinding()) return;

            if (_hasValue && EqualityComparer<TValue>.Default.Equals(Value, value)) return;

            _hasValue = true;

            await SetCurrentValueAsync(value);

            await OnSelectItem.InvokeAsync(item);
        }
    }

    private void OnSetValues()
    {
        NotifyFieldChanged();
    }

    private async Task HandleOnItemClick(int index)
    {
        var item = _items[index];

        if (Disabled || GetIsDisabled(item)) return;

        _activeIndex = index;

        await SelectAsync(item);
    }

    private async Task HandleOnKeyDown(KeyboardEventArgs e)
    {
        if (Disabled || _items.Count == 0) return;

        var current = TabStopIndex;

        if (e.Key is " " or "Enter")
        {
            if (e.ShiftKey || e.CtrlKey || e.AltKey || e.MetaKey || current < 0) return;

            await SelectAsync(_items[current]);
            return;
        }

        int target;

        switch (e.Key)
        {
            case "ArrowDown":
                target = FindEnabled(current, 1);
                break;
            case "ArrowUp":
                target = FindEnabled(current, -1);
                break;
            case "Home":
                target = FindEnabled(-1, 1);
                break;
            case "End":
                target = FindEnabled(_items.Count, -1);
                break;
            default:
                if (e.Key.Length != 1 || e.CtrlKey || e.AltKey || e.MetaKey) return;
                target = FindByTypeahead(e.Key, current);
                break;
        }

        if (target < 0 || target == current) return;

        _activeIndex = target;
        _focusActiveAfterRender = true;

        // The selection follows the keyboard in single select mode, the way a native list does; with several to
        // choose, moving only moves, and Space is what chooses.
        if (MultiSelect is false)
        {
            await SelectAsync(_items[target]);
        }
    }

    // The next item from the given index in the given direction that can be selected, or -1 at the end of the list.
    private int FindEnabled(int from, int step)
    {
        for (var i = from + step; i >= 0 && i < _items.Count; i += step)
        {
            if (GetIsDisabled(_items[i]) is false) return i;
        }

        return -1;
    }

    // Typing the start of a text moves to the next item whose text starts with what has been typed so far; a pause
    // starts over. The same letter typed again moves on to the next item that starts with it.
    private int FindByTypeahead(string key, int current)
    {
        var now = DateTime.UtcNow;

        if (now - _typeaheadAt > _typeaheadTimeout) _typeahead = string.Empty;

        _typeaheadAt = now;
        _typeahead += key;

        var search = _typeahead.All(c => char.ToUpperInvariant(c) == char.ToUpperInvariant(_typeahead[0])) ? _typeahead[..1] : _typeahead;
        var start = search.Length == 1 ? current + 1 : Math.Max(current, 0);

        for (var k = 0; k < _items.Count; k++)
        {
            var i = (start + k) % _items.Count;
            var item = _items[i];

            if (GetIsDisabled(item)) continue;

            if ((GetText(item) ?? string.Empty).StartsWith(search, StringComparison.CurrentCultureIgnoreCase)) return i;
        }

        return -1;
    }

    private string GetOptionId(int index) => $"{_listId}-{index}";

    private string GetItemClass(TItem item, bool selected, bool disabled)
    {
        var classes = new List<string> { "bit-lbx-itm" };

        if (selected)
        {
            classes.Add("bit-lbx-sel");
            classes.AddIfHasValue(Classes?.SelectedItem);
        }

        if (disabled) classes.Add("bit-lbx-ids");

        classes.AddIfHasValue(Classes?.Item);
        classes.AddIfHasValue(GetClass(item));

        return string.Join(" ", classes);
    }

    private string? GetItemStyle(TItem item, bool selected)
    {
        var style = string.Join(";", new[] { Styles?.Item, selected ? Styles?.SelectedItem : null, GetStyle(item) }.Where(s => s.HasValue()));

        return style.HasValue() ? style : null;
    }

    private string? GetListStyle()
    {
        var style = string.Join(";", new[] { MaxHeight.HasValue() ? $"max-height:{MaxHeight}" : null, Styles?.List }.Where(s => s.HasValue()));

        return style.HasValue() ? style : null;
    }

    private ValueTask FocusItemAsync(int index, bool preventScroll)
    {
        if (index < 0 || index >= _items.Count) return ValueTask.CompletedTask;

        return _optionRefs.TryGetValue(GetItemKey(_items[index], index), out var element)
            ? element.FocusAsync(preventScroll)
            : ValueTask.CompletedTask;
    }

    private string GetItemKey(TItem item, int index) => GetKey(item) ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture);



    // The item readers, for each of the three ways the items are given: BitListBoxItem objects, BitListBoxOption
    // components, or objects of a custom class read through NameSelectors - by a selector when one is given, and by the
    // name of the member otherwise. A custom item with no NameSelectors has nothing to be read by.

    private T? Read<T>(TItem item,
                       Func<BitListBoxItem<TValue>, T?> fromItem,
                       Func<BitListBoxOption<TValue>, T?> fromOption,
                       BitNameSelectorPair<TItem, T>? selector)
    {
        if (item is BitListBoxItem<TValue> listBoxItem) return fromItem(listBoxItem);

        if (item is BitListBoxOption<TValue> listBoxOption) return fromOption(listBoxOption);

        if (selector is null) return default;

        return selector.Selector is not null
            ? selector.Selector(item)
            : item.GetValueFromProperty<T>(selector.Name);
    }

    private string? GetAriaLabel(TItem item) => Read(item, i => i.AriaLabel, o => o.AriaLabel, NameSelectors?.AriaLabel);

    private string? GetClass(TItem item) => Read(item, i => i.Class, o => o.Class, NameSelectors?.Class);

    private string? GetDescription(TItem item) => Read(item, i => i.Description, o => o.Description, NameSelectors?.Description);

    private BitIconInfo? GetIcon(TItem item) => BitIconInfo.From(Read(item, i => i.Icon, o => o.Icon, NameSelectors?.Icon),
                                                                 Read(item, i => i.IconName, o => o.IconName, NameSelectors?.IconName));

    private bool GetIsDisabled(TItem item) => Read(item, i => i.IsDisabled, o => o.IsDisabled, NameSelectors?.IsDisabled);

    private string? GetKey(TItem item) => Read(item, i => i.Key, o => o.Key, NameSelectors?.Key);

    private string? GetStyle(TItem item) => Read(item, i => i.Style, o => o.Style, NameSelectors?.Style);

    private string? GetText(TItem item) => Read(item, i => i.Text, o => o.Text, NameSelectors?.Text);

    private string? GetTitle(TItem item) => Read(item, i => i.Title, o => o.Title, NameSelectors?.Title);

    private TValue? GetValue(TItem item) => Read(item, i => i.Value, o => o.Value, NameSelectors?.Value);
}
