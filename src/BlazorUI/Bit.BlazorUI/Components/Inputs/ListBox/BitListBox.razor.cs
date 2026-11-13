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
/// </remarks>
public partial class BitListBox<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem, TValue> : BitInputBase<TValue> where TItem : class
{
    // The keys the list box answers itself, whose default action (scrolling the page) is kept from happening.
    private static readonly string[] _navigationKeys = ["ArrowUp", "ArrowDown", "Home", "End", " "];

    // How long a pause in typing starts a new typeahead search.
    private static readonly TimeSpan _typeaheadTimeout = TimeSpan.FromMilliseconds(1000);



    private List<TItem> _items = [];
    private IEnumerable<TItem>? _oldItems;
    private ElementReference[] _optionRefs = [];

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

        return index < 0 ? ValueTask.CompletedTask : _optionRefs[index].FocusAsync(preventScroll);
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

        if (ReferenceEquals(_oldItems, Items) is false)
        {
            _oldItems = Items;
            _items = Items?.ToList() ?? [];
            _optionRefs = new ElementReference[_items.Count];

            if (_activeIndex >= _items.Count) _activeIndex = -1;
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
            if (index >= 0) await _optionRefs[index].FocusAsync();
        }
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

    private string GetItemKey(TItem item, int index) => GetKey(item) ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture);



    // The item readers. Only BitListBoxItem is read for now; the readers are where the other item types will be read.

    private BitListBoxItem<TValue>? AsItem(TItem item) => item as BitListBoxItem<TValue>;

    private string? GetAriaLabel(TItem item) => AsItem(item)?.AriaLabel;

    private string? GetClass(TItem item) => AsItem(item)?.Class;

    private string? GetDescription(TItem item) => AsItem(item)?.Description;

    private BitIconInfo? GetIcon(TItem item) => BitIconInfo.From(AsItem(item)?.Icon, AsItem(item)?.IconName);

    private bool GetIsDisabled(TItem item) => AsItem(item)?.IsDisabled ?? false;

    private string? GetKey(TItem item) => AsItem(item)?.Key;

    private string? GetStyle(TItem item) => AsItem(item)?.Style;

    private string? GetText(TItem item) => AsItem(item)?.Text;

    private string? GetTitle(TItem item) => AsItem(item)?.Title;

    private TValue? GetValue(TItem item) => AsItem(item) is { } listBoxItem ? listBoxItem.Value : default;
}
