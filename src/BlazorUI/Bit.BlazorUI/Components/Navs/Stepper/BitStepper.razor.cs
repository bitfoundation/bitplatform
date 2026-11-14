using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// BitStepper shows the steps of a process in their order - the ones done, the one the user is on and the ones still
/// to come - horizontally or vertically, and lets the user go to a step by clicking it.
/// </summary>
/// <remarks>
/// The stepper draws the steps alone: what each step shows, and the buttons that move between them, are the page's,
/// which moves the stepper with <see cref="CurrentIndex"/>. A <see cref="Linear"/> stepper lets the user go back to a
/// step already done but not skip ahead, and a <see cref="ReadOnly"/> one only shows where the process is.
/// <br />
/// The steps are given in one of three ways: <see cref="BitStepperItem"/> objects in <see cref="Items"/>, objects of a
/// custom class in <see cref="Items"/> read through <see cref="NameSelectors"/>, or <see cref="BitStepperOption"/>
/// components in <see cref="Options"/> (or as the child content).
/// </remarks>
public partial class BitStepper<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TItem> : BitComponentBase where TItem : class
{
    private const string DefaultCompletedIconName = "CheckMark";
    private const string DefaultErrorIconName = "ErrorBadge";



    private List<TItem> _items = [];
    private IEnumerable<TItem>? _oldItems;

    // Set when an option registers or goes away, so the order of the steps is read back from the document after the
    // render (see ReorderOptions).
    private bool _optionsOrderDirty;
    private string _optionsContainerId = default!;

    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// The default values of the parameters of the stepper, cascaded through a <see cref="BitParams"/>. A value set on the
    /// stepper itself wins over it.
    /// </summary>
    [CascadingParameter(Name = BitStepperParams.ParamName)]
    public BitStepperParams? CascadingParameters { get; set; }



    /// <summary>
    /// The content of the stepper, that are BitStepperOption components.
    /// </summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Custom CSS classes for the different parts of the stepper.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitStepperClassStyles? Classes { get; set; }

    /// <summary>
    /// The icon shown in the indicator of a completed step, using custom CSS classes for external icon libraries. Takes
    /// precedence over <see cref="CompletedIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? CompletedIcon { get; set; }

    /// <summary>
    /// The name of the icon shown in the indicator of a completed step, from the built-in Fluent UI icons. Defaults to
    /// "CheckMark".
    /// </summary>
    [Parameter] public string? CompletedIconName { get; set; }

    /// <summary>
    /// The index of the current step. The steps before it are completed and the ones after it are upcoming; an index
    /// past the last step shows every step completed, and a negative one shows none started. Default: 0.
    /// </summary>
    [Parameter, TwoWayBound]
    public int CurrentIndex { get; set; }

    /// <summary>
    /// The icon shown in the indicator of a step in error, using custom CSS classes for external icon libraries. Takes
    /// precedence over <see cref="ErrorIconName"/> when both are set.
    /// </summary>
    [Parameter] public BitIconInfo? ErrorIcon { get; set; }

    /// <summary>
    /// The name of the icon shown in the indicator of a step in error, from the built-in Fluent UI icons. Defaults to
    /// "ErrorBadge".
    /// </summary>
    [Parameter] public string? ErrorIconName { get; set; }

    /// <summary>
    /// The steps of the stepper.
    /// </summary>
    [Parameter] public IEnumerable<TItem> Items { get; set; } = [];

    /// <summary>
    /// Keeps the user from skipping ahead: only the steps before the current one (and the current one) can be clicked,
    /// so a step is reached only by the page moving <see cref="CurrentIndex"/> forward.
    /// </summary>
    [Parameter] public bool Linear { get; set; }

    /// <summary>
    /// The names (or selectors) of the members the steps are read from, for items of a custom class.
    /// </summary>
    [Parameter] public BitStepperNameSelectors<TItem>? NameSelectors { get; set; }

    /// <summary>
    /// The callback for when the user goes to another step, with its index. It runs even when
    /// <see cref="CurrentIndex"/> is bound one way and could not be written back, so a page holding the index itself
    /// still hears about the step the user asked for.
    /// </summary>
    [Parameter] public EventCallback<int> OnChange { get; set; }

    /// <summary>
    /// The callback for when a step is clicked, the current one included, as long as it can be gone to.
    /// </summary>
    [Parameter] public EventCallback<TItem> OnItemClick { get; set; }

    /// <summary>
    /// Alias for the ChildContent parameter: the steps declared as BitStepperOption components.
    /// </summary>
    [Parameter] public RenderFragment? Options { get; set; }

    /// <summary>
    /// Only shows where the process is: no step can be clicked.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool ReadOnly { get; set; }

    /// <summary>
    /// Custom CSS styles for the different parts of the stepper.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitStepperClassStyles? Styles { get; set; }

    /// <summary>
    /// Lays the steps out from top to bottom instead of from start to end.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Vertical { get; set; }



    protected override string RootElementClass => "bit-stp";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Vertical ? "bit-stp-vrt" : "bit-stp-hrz");

        ClassBuilder.Register(() => ReadOnly ? "bit-stp-rdo" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override void OnInitialized()
    {
        _optionsContainerId = $"BitStepper-{UniqueId}-options";

        base.OnInitialized();
    }

    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        // With options, the steps are the options that registered themselves; Items is not read at all.
        if (ChildContent is null && Options is null && ReferenceEquals(_oldItems, Items) is false)
        {
            _oldItems = Items;
            _items = Items?.ToList() ?? [];
        }

        base.OnParametersSet();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (_optionsOrderDirty is false) return;

        _optionsOrderDirty = false;

        try
        {
            var orderedOptionIds = await _js.BitUtilsGetChildrenAttributes(_optionsContainerId, BitStepperOption._OPTION_ID_ATTRIBUTE);

            if (IsDisposed || orderedOptionIds is null) return;

            ReorderOptions(orderedOptionIds);
        }
        catch (JSDisconnectedException) { } // the circuit is gone, so there is no page left to order the steps for
        catch (JSException) { } // reading the markers failed, which only leaves the steps in the order they registered
    }



    internal void RegisterOption(BitStepperOption option)
    {
        _items.Add((option as TItem)!);
        _optionsOrderDirty = true;

        StateHasChanged();
    }

    internal void UnregisterOption(BitStepperOption option)
    {
        // The options unregister while they are disposed, which also happens when the stepper itself goes away, so a
        // render is only asked for while the stepper is still alive.
        if (IsDisposing) return;

        _items.Remove((option as TItem)!);
        _optionsOrderDirty = true;

        StateHasChanged();
    }

    // The stepper draws the steps from the options and reads their values while it renders, so a change to the
    // parameters of an option needs a render of the stepper to show.
    internal void NotifyOptionParametersChanged()
    {
        if (IsDisposed) return;

        StateHasChanged();
    }

    // An option rendered after the others (an @if that turns true) registers last, wherever it sits in the markup, and
    // the position of a step is what numbers it and decides its state. So the steps are put back in the order of the
    // options' markers in the document, which is the order of the markup.
    internal void ReorderOptions(string[] orderedOptionIds)
    {
        if (orderedOptionIds.Length == 0) return;

        List<TItem> ordered = new(_items.Count);

        foreach (var optionId in orderedOptionIds)
        {
            var item = _items.FirstOrDefault(i => (i as BitStepperOption)?._OptionId == optionId);
            if (item is null || ordered.Contains(item)) continue;

            ordered.Add(item);
        }

        ordered.AddRange(_items.Except(ordered));

        if (ordered.SequenceEqual(_items)) return;

        _items = ordered;

        StateHasChanged();
    }



    private BitStepperStepState GetState(TItem item, int index)
    {
        if (GetIsError(item)) return BitStepperStepState.Error;

        if (index < CurrentIndex) return BitStepperStepState.Completed;

        return index == CurrentIndex ? BitStepperStepState.Current : BitStepperStepState.Upcoming;
    }

    // Whether a click on the step goes anywhere: never on a read-only stepper or a disabled step, and on a linear one
    // only back (or to the current step), since going forward is the page's to do.
    private bool CanGoTo(TItem item, int index)
    {
        if (ReadOnly || Disabled || GetIsDisabled(item)) return false;

        return Linear is false || index <= CurrentIndex;
    }

    // A step is a button whenever a click on it could ever go somewhere. A disabled one stays a (disabled) button, so it
    // is announced as unavailable rather than dropped; a step a linear stepper does not let the user reach yet is no
    // control at all, since there is nothing to press there.
    private bool IsButton(TItem item, int index)
    {
        if (ReadOnly) return false;

        return Linear is false || index <= CurrentIndex || Disabled || GetIsDisabled(item);
    }

    private async Task HandleOnStepClick(TItem item, int index)
    {
        if (CanGoTo(item, index) is false) return;

        if (item is BitStepperItem stepperItem)
        {
            stepperItem.OnClick?.Invoke(stepperItem);
        }
        else if (item is BitStepperOption stepperOption)
        {
            await stepperOption.OnClick.InvokeAsync(stepperOption);
        }
        else if (NameSelectors is not null)
        {
            Read(item, null, null, NameSelectors.OnClick)?.Invoke(item);
        }

        await OnItemClick.InvokeAsync(item);

        if (index == CurrentIndex) return;

        await AssignCurrentIndex(index);

        await OnChange.InvokeAsync(index);
    }

    private BitIconInfo? GetIndicatorIcon(TItem item, BitStepperStepState state)
    {
        return state switch
        {
            BitStepperStepState.Error => BitIconInfo.From(ErrorIcon, ErrorIconName ?? DefaultErrorIconName),
            BitStepperStepState.Completed => BitIconInfo.From(CompletedIcon, CompletedIconName ?? DefaultCompletedIconName),
            _ => BitIconInfo.From(GetIcon(item), GetIconName(item))
        };
    }

    // What a screen reader is told about a step on top of its text: the current one says so through aria-current.
    private static string? GetStateText(BitStepperStepState state)
    {
        return state switch
        {
            BitStepperStepState.Completed => BitStrings.Current.StepCompleted,
            BitStepperStepState.Error => BitStrings.Current.StepError,
            _ => null
        };
    }

    private static string GetNumber(int index) => (index + 1).ToString(CultureInfo.CurrentCulture);

    private string GetStepClass(TItem item, BitStepperStepState state, bool disabled)
    {
        var classes = new List<string> { "bit-stp-stp" };

        switch (state)
        {
            case BitStepperStepState.Completed:
                classes.Add("bit-stp-cmp");
                classes.AddIfHasValue(Classes?.CompletedStep);
                break;
            case BitStepperStepState.Current:
                classes.Add("bit-stp-cur");
                classes.AddIfHasValue(Classes?.CurrentStep);
                break;
            case BitStepperStepState.Upcoming:
                classes.Add("bit-stp-upc");
                classes.AddIfHasValue(Classes?.UpcomingStep);
                break;
            case BitStepperStepState.Error:
                classes.Add("bit-stp-err");
                classes.AddIfHasValue(Classes?.ErrorStep);
                break;
        }

        if (disabled) classes.Add("bit-stp-sdi");

        classes.AddIfHasValue(Classes?.Step);
        classes.AddIfHasValue(GetClass(item));

        return string.Join(" ", classes);
    }

    private string? GetStepStyle(TItem item, BitStepperStepState state)
    {
        var stateStyle = state switch
        {
            BitStepperStepState.Completed => Styles?.CompletedStep,
            BitStepperStepState.Current => Styles?.CurrentStep,
            BitStepperStepState.Upcoming => Styles?.UpcomingStep,
            BitStepperStepState.Error => Styles?.ErrorStep,
            _ => null
        };

        var style = string.Join(";", new[] { Styles?.Step, stateStyle, GetStyle(item) }.Where(s => s.HasValue()));

        return style.HasValue() ? style : null;
    }

    private string GetItemKey(TItem item, int index) => GetKey(item) ?? index.ToString(CultureInfo.InvariantCulture);



    // The item readers, for each of the three ways the steps are given: BitStepperItem objects, BitStepperOption
    // components, or objects of a custom class read through NameSelectors - by a selector when one is given, and by the
    // name of the member otherwise. A custom item with no NameSelectors has nothing to be read by.

    private T? Read<T>(TItem item,
                       Func<BitStepperItem, T?>? fromItem,
                       Func<BitStepperOption, T?>? fromOption,
                       BitNameSelectorPair<TItem, T>? selector)
    {
        if (item is BitStepperItem stepperItem) return fromItem is null ? default : fromItem(stepperItem);

        if (item is BitStepperOption stepperOption) return fromOption is null ? default : fromOption(stepperOption);

        if (selector is null) return default;

        return selector.Selector is not null
            ? selector.Selector(item)
            : item.GetValueFromProperty<T>(selector.Name);
    }

    private string? GetAriaLabel(TItem item) => Read(item, i => i.AriaLabel, o => o.AriaLabel, NameSelectors?.AriaLabel);

    private string? GetClass(TItem item) => Read(item, i => i.Class, o => o.Class, NameSelectors?.Class);

    private string? GetDescription(TItem item) => Read(item, i => i.Description, o => o.Description, NameSelectors?.Description);

    private BitIconInfo? GetIcon(TItem item) => Read(item, i => i.Icon, o => o.Icon, NameSelectors?.Icon);

    private string? GetIconName(TItem item) => Read(item, i => i.IconName, o => o.IconName, NameSelectors?.IconName);

    private bool GetIsDisabled(TItem item) => Read(item, i => i.IsDisabled, o => o.IsDisabled, NameSelectors?.IsDisabled);

    private bool GetIsError(TItem item) => Read(item, i => i.IsError, o => o.IsError, NameSelectors?.IsError);

    private string? GetKey(TItem item) => Read(item, i => i.Key, o => o.Key, NameSelectors?.Key);

    private string? GetStyle(TItem item) => Read(item, i => i.Style, o => o.Style, NameSelectors?.Style);

    private string? GetText(TItem item) => Read(item, i => i.Text, o => o.Text, NameSelectors?.Text);

    private string? GetTitle(TItem item) => Read(item, i => i.Title, o => o.Title, NameSelectors?.Title);
}
